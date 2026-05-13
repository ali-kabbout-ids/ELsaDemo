using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Parameters;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PurchaseOrderApi.Activities;
using PurchaseOrderApi.Dtos;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PurchaseOrderApi.Services;

public sealed class WorkflowInboxService(
    IBookmarkStore bookmarkStore,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowRuntime workflowRuntime,
    IActivityExecutionStore activityExecutionStore)  // ADD THIS
{
    /// <summary>
    /// Returns all pending approval bookmarks for the given role.
    /// </summary>
    /// 
    private const string ActivityTypeName =
    "ApplicationFlow.WaitForApplicationApprovalActivity";

    public async Task<List<InboxItemDto>> GetPendingItemsAsync(string role)
    {
        // BookmarkFilter does not support filtering by activity type name directly in this Elsa version.
        // We'll query bookmarks and filter in-memory by StoredBookmark.ActivityTypeName.
        //IEnumerable<Elsa.Workflows.Runtime.Entities.StoredBookmark> bookmarks = (await bookmarkStore.FindManyAsync(
        //    new BookmarkFilter(),
        //    CancellationToken.None))
        //    .Where(x => string.Equals(x.ActivityTypeName, nameof(WaitForApplicationApprovalActivity), StringComparison.OrdinalIgnoreCase));



        var allBookmarks = await bookmarkStore.FindManyAsync(
          new BookmarkFilter(),
          CancellationToken.None);

        var bookmarks = allBookmarks
            .Where(x => x.ActivityTypeName == ActivityTypeName)
            .ToList();


        List<InboxItemDto> results = new();

        foreach (Elsa.Workflows.Runtime.Entities.StoredBookmark bookmark in bookmarks)
        {
            if (string.IsNullOrWhiteSpace(bookmark.ActivityInstanceId))
                continue;

            Elsa.Workflows.Management.Entities.WorkflowInstance? instance = await workflowInstanceStore.FindAsync(
                new WorkflowInstanceFilter { Id = bookmark.WorkflowInstanceId },
                CancellationToken.None);

            if (instance is null)
                continue;

            JsonNode? state = ToJsonNode(instance.WorkflowState);
            if (state is null)
                continue;

            JsonNode? activityContext = FindActivityExecutionContextNode(state, bookmark.ActivityInstanceId);
            if (activityContext is null)
                continue;

            string? requiredRole = FindFirstString(activityContext, "RequiredRole");
            if (string.IsNullOrWhiteSpace(requiredRole))
                continue;

            if (!string.IsNullOrWhiteSpace(role) &&
                !string.Equals(requiredRole, role, StringComparison.OrdinalIgnoreCase))
                continue;

            string stepName = FindFirstString(activityContext, "StepName") ?? string.Empty;

            int? appIdFromActivity = FindFirstInt(activityContext, "ApplicationId");
            int? appIdFromVariables = FindWorkflowVariableInt(state, "ApplicationId");
            int appId = appIdFromActivity ?? appIdFromVariables ?? 0;

            results.Add(new InboxItemDto(
                WorkflowInstanceId: bookmark.WorkflowInstanceId,
                BookmarkId: bookmark.Id,
                ApplicationId: appId,
                StepName: stepName,
                RequiredRole: requiredRole
            ));
        }

        return results;
    }

    /// <summary>
    /// Submits an approval decision for the specified bookmark and resumes the workflow.
    /// </summary>
    public async Task SubmitDecisionAsync(string bookmarkId, string decision, string reason)
    {
        Elsa.Workflows.Runtime.Entities.StoredBookmark? bookmark = await bookmarkStore.FindAsync(
            new BookmarkFilter { BookmarkId = bookmarkId },
            CancellationToken.None);

        if (bookmark is null)
            throw new KeyNotFoundException($"Bookmark '{bookmarkId}' not found.");

        await workflowRuntime.ResumeWorkflowAsync(
            bookmark.WorkflowInstanceId,
            new ResumeWorkflowRuntimeParams
            {
                BookmarkId = bookmark.Id,
                Input = new Dictionary<string, object>
                {
                    ["decision"] = decision,
                    ["reason"] = reason
                }
            });
    }

    private static JsonNode? ToJsonNode(object? workflowState)
    {
        if (workflowState is null)
            return null;

        try
        {
            // WorkflowState is a complex object in Elsa. Serializing it and walking JSON
            // is the most resilient approach across Elsa versions and storage formats.
            string json = JsonSerializer.Serialize(workflowState);
            return JsonNode.Parse(json);
        }
        catch
        {
            return null;
        }
    }

    private static JsonNode? FindActivityExecutionContextNode(JsonNode root, string activityInstanceId)
    {
        // Try the most likely keys first: ActivityInstanceId and Id.
        return FindFirstObjectByPropertyValue(root, "ActivityInstanceId", activityInstanceId)
               ?? FindFirstObjectByPropertyValue(root, "Id", activityInstanceId);
    }

    private static JsonNode? FindFirstObjectByPropertyValue(JsonNode node, string propertyName, string propertyValue)
    {
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue(propertyName, out JsonNode? valueNode))
            {
                string? val = valueNode?.GetValue<string?>();
                if (string.Equals(val, propertyValue, StringComparison.OrdinalIgnoreCase))
                    return obj;
            }

            foreach (KeyValuePair<string, JsonNode?> kvp in obj)
            {
                if (kvp.Value is null) continue;
                JsonNode? found = FindFirstObjectByPropertyValue(kvp.Value, propertyName, propertyValue);
                if (found is not null) return found;
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (JsonNode? item in arr)
            {
                if (item is null) continue;
                JsonNode? found = FindFirstObjectByPropertyValue(item, propertyName, propertyValue);
                if (found is not null) return found;
            }
        }

        return null;
    }

    private static string? FindFirstString(JsonNode node, string key)
    {
        JsonNode? found = FindFirstProperty(node, key);
        if (found is null) return null;

        try
        {
            if (found is JsonValue)
                return found.GetValue<string?>();

            // Some Elsa states store inputs as objects like { "Value": "..." }.
            if (found is JsonObject obj && obj.TryGetPropertyValue("Value", out JsonNode? inner) && inner is not null)
                return inner.GetValue<string?>();
        }
        catch
        {
            // ignored
        }

        return found.ToString();
    }

    private static int? FindFirstInt(JsonNode node, string key)
    {
        JsonNode? found = FindFirstProperty(node, key);
        if (found is null) return null;

        try
        {
            if (found is JsonValue)
                return found.GetValue<int?>();

            if (found is JsonObject obj && obj.TryGetPropertyValue("Value", out JsonNode? inner) && inner is not null)
                return inner.GetValue<int?>();
        }
        catch
        {
            // ignored
        }

        if (int.TryParse(found.ToString(), out int value))
            return value;

        return null;
    }

    private static int? FindWorkflowVariableInt(JsonNode workflowState, string variableName)
    {
        // Variables are usually stored under a "Variables" node. We'll look for an object
        // with the variable name, then parse a primitive or nested "Value".
        JsonNode? varsNode = FindFirstProperty(workflowState, "Variables");
        if (varsNode is null) return null;

        if (varsNode is JsonObject varsObj && varsObj.TryGetPropertyValue(variableName, out JsonNode? varNode) && varNode is not null)
        {
            if (varNode is JsonValue)
                return varNode.GetValue<int?>();

            if (varNode is JsonObject varObj)
            {
                if (varObj.TryGetPropertyValue("Value", out JsonNode? inner) && inner is not null)
                {
                    try { return inner.GetValue<int?>(); } catch { /* ignored */ }
                    if (int.TryParse(inner.ToString(), out int parsed)) return parsed;
                }
            }

            if (int.TryParse(varNode.ToString(), out int parsed2))
                return parsed2;
        }

        return null;
    }

    private static JsonNode? FindFirstProperty(JsonNode node, string key)
    {
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue(key, out JsonNode? value))
                return value;

            foreach (KeyValuePair<string, JsonNode?> kvp in obj)
            {
                if (kvp.Value is null) continue;
                JsonNode? found = FindFirstProperty(kvp.Value, key);
                if (found is not null) return found;
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (JsonNode? item in arr)
            {
                if (item is null) continue;
                JsonNode? found = FindFirstProperty(item, key);
                if (found is not null) return found;
            }
        }

        return null;
    }
}

