using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Parameters;
using PurchaseOrderApi.Dtos;
using PurchaseOrderApi.Enums;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PurchaseOrderApi.Services;

public sealed class WorkflowInboxService(
    IBookmarkStore bookmarkStore,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowRuntime workflowRuntime,
    IActivityExecutionStore activityExecutionStore)
{
    private const string ActivityTypeName =
        "ApplicationFlow.WaitForApplicationApprovalActivity";

    /// <summary>
    /// Returns all pending approval bookmarks for the given role.
    /// </summary>
    public async Task<List<InboxItemDto>> GetPendingItemsAsync(string role)
    {
        IEnumerable<Elsa.Workflows.Runtime.Entities.StoredBookmark> allBookmarks =
            await bookmarkStore.FindManyAsync(new BookmarkFilter(), CancellationToken.None);

        List<Elsa.Workflows.Runtime.Entities.StoredBookmark> bookmarks = allBookmarks
            .Where(x => string.Equals(x.ActivityTypeName, ActivityTypeName, StringComparison.Ordinal))
            .ToList();

        List<InboxItemDto> results = new();

        foreach (Elsa.Workflows.Runtime.Entities.StoredBookmark bookmark in bookmarks)
        {
            if (string.IsNullOrWhiteSpace(bookmark.ActivityInstanceId))
                continue;

            // Bookmark.ActivityInstanceId is the *activity execution record* id (same as ActivityExecutionRecords.Id
            // in SQL), not ActivityNodeId (graph node) nor ActivityId (definition node id, e.g. WaitI3lamKanouni).
            Elsa.Workflows.Runtime.Entities.ActivityExecutionRecord? executionRecord =
                await activityExecutionStore.FindAsync(
                    new ActivityExecutionRecordFilter
                    {
                        WorkflowInstanceId = bookmark.WorkflowInstanceId,
                        Id = bookmark.ActivityInstanceId
                    },
                    CancellationToken.None);

            if (executionRecord is null ||
                executionRecord.ActivityState is null ||
                executionRecord.ActivityState.Count == 0)
                continue;

            JsonNode? activityState = ParseActivityStateToJson(executionRecord.ActivityState);
            if (activityState is null)
                continue;

            string? requiredRole = GetJsonString(activityState["RequiredRole"]);
            if (string.IsNullOrWhiteSpace(requiredRole))
                continue;

            if (!string.IsNullOrWhiteSpace(role) &&
                !string.Equals(requiredRole, role, StringComparison.OrdinalIgnoreCase))
                continue;

            string stepName = GetJsonString(activityState["StepName"]) ?? string.Empty;

            int appId = 0;
            string? appIdStr = GetJsonString(activityState["ApplicationId"]);
            if (!int.TryParse(appIdStr, out appId))
            {
                Elsa.Workflows.Management.Entities.WorkflowInstance? instance =
                    await workflowInstanceStore.FindAsync(
                        new WorkflowInstanceFilter { Id = bookmark.WorkflowInstanceId },
                        CancellationToken.None);

                if (instance is not null)
                {
                    JsonNode? stateJson = JsonNode.Parse(JsonSerializer.Serialize(instance.WorkflowState));
                    if (stateJson is not null)
                        appId = FindWorkflowVariableInt(stateJson, "ApplicationId") ?? 0;
                }
            }

            JsonNode? actionsNode = activityState?["AllowedActionKeys"];
            ICollection<WorkflowAction> availableActions = new List<WorkflowAction>();

            if (actionsNode is JsonArray actionsArray)
            {
                availableActions = actionsArray
                    .Select(node => GetJsonString(node))         
                    .Where(key => !string.IsNullOrWhiteSpace(key))
                    .Select(key => WorkflowActions.FindByKey(key!))
                    .Where(a => a is not null)
                    .Select(a => a!)
                    .ToList();
            }
            else if (actionsNode is JsonValue jsonValue)
            {
                // Fallback: stored as comma-separated string
                string? raw = GetJsonString(jsonValue);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    availableActions = raw
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(key => WorkflowActions.FindByKey(key))
                        .Where(a => a is not null)
                        .Select(a => a!)
                        .ToList();
                }
            }

            JsonNode? sectionsNode = activityState?["VisibleSections"];
            ICollection<WorkflowSection> availableSections = new List<WorkflowSection>();

            if (sectionsNode is JsonArray sectionsArray)
            {
                availableSections = sectionsArray
                    .Select(node => GetJsonString(node))
                    .Where(key => !string.IsNullOrWhiteSpace(key))
                    .Select(key => WorkflowSections.FindByKey(key!))
                    .Where(s => s is not null)
                    .Select(s => s!)
                    .ToList();
            }
            else if (sectionsNode is JsonValue sectionValue)
            {
                string? raw = GetJsonString(sectionValue);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    availableSections = raw
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(key => WorkflowSections.FindByKey(key))
                        .Where(s => s is not null)
                        .Select(s => s!)
                        .ToList();
                }
            }

            results.Add(new InboxItemDto(
                WorkflowInstanceId: bookmark.WorkflowInstanceId,
                BookmarkId: bookmark.Id,
                ApplicationId: appId,
                StepName: stepName,
                RequiredRole: requiredRole,
                AvailableActions: availableActions,
                AvailableSections: availableSections
            ));
        }

        return results;
    }

    /// <summary>
    /// Returns whether the given role matches the <c>RequiredRole</c> stored on the activity for this bookmark.
    /// </summary>
    public async Task<bool> ValidateRoleAsync(string bookmarkId, string role)
    {
        Elsa.Workflows.Runtime.Entities.StoredBookmark? bookmark = await bookmarkStore.FindAsync(
            new BookmarkFilter { BookmarkId = bookmarkId },
            CancellationToken.None);

        if (bookmark is null || string.IsNullOrWhiteSpace(bookmark.ActivityInstanceId))
            return false;

        Elsa.Workflows.Runtime.Entities.ActivityExecutionRecord? executionRecord =
            await activityExecutionStore.FindAsync(
                new ActivityExecutionRecordFilter
                {
                    WorkflowInstanceId = bookmark.WorkflowInstanceId,
                    Id = bookmark.ActivityInstanceId
                },
                CancellationToken.None);

        if (executionRecord is null ||
            executionRecord.ActivityState is null ||
            executionRecord.ActivityState.Count == 0)
            return false;

        JsonNode? activityState = ParseActivityStateToJson(executionRecord.ActivityState);
        string? requiredRole = GetJsonString(activityState?["RequiredRole"]);

        return string.Equals(requiredRole, role, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Submits an approval decision for the specified bookmark and resumes the workflow.
    /// </summary>
    public async Task SubmitDecisionAsync(
        string bookmarkId,
        string action,
        string? reason,
        Dictionary<string, object>? extra = null)
    {
        StoredBookmark? bookmark = await bookmarkStore.FindAsync(
            new BookmarkFilter { BookmarkId = bookmarkId }, CancellationToken.None);

        if (bookmark is null)
            throw new KeyNotFoundException($"Bookmark '{bookmarkId}' not found.");

        Dictionary<string, object> payload = new()
        {
            ["action"] = action,
            ["reason"] = reason ?? string.Empty
        };

        if (extra is not null)
            foreach (KeyValuePair<string, object> kv in extra)
                payload[kv.Key] = kv.Value;

        await workflowRuntime.ResumeWorkflowAsync(
            bookmark.WorkflowInstanceId,
            new ResumeWorkflowRuntimeParams
            {
                BookmarkId = bookmark.Id,
                Input = payload
            });
    }

    private static JsonNode? ParseActivityStateToJson(
        System.Collections.Generic.IDictionary<string, object?> activityState)
    {
        try
        {
            return JsonNode.Parse(JsonSerializer.Serialize(activityState));
        }
        catch
        {
            return null;
        }
    }

    private static string? GetJsonString(JsonNode? node)
    {
        if (node is null)
            return null;

        if (node is JsonValue value)
        {
            try
            {
                return value.GetValue<string?>();
            }
            catch
            {
                return value.ToJsonString().Trim('"');
            }
        }

        return node.ToString();
    }

    private static int? FindWorkflowVariableInt(JsonNode workflowState, string variableName)
    {
        JsonNode? varsNode = FindFirstProperty(workflowState, "Variables");
        if (varsNode is null)
            return null;

        if (varsNode is JsonObject varsObj &&
            varsObj.TryGetPropertyValue(variableName, out JsonNode? varNode) &&
            varNode is not null)
        {
            if (varNode is JsonValue)
                return varNode.GetValue<int?>();

            if (varNode is JsonObject varObj)
            {
                if (varObj.TryGetPropertyValue("Value", out JsonNode? inner) && inner is not null)
                {
                    try
                    {
                        return inner.GetValue<int?>();
                    }
                    catch
                    {
                        // ignored
                    }

                    if (int.TryParse(inner.ToString(), out int parsed))
                        return parsed;
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
                if (kvp.Value is null)
                    continue;

                JsonNode? found = FindFirstProperty(kvp.Value, key);
                if (found is not null)
                    return found;
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (JsonNode? item in arr)
            {
                if (item is null)
                    continue;

                JsonNode? found = FindFirstProperty(item, key);
                if (found is not null)
                    return found;
            }
        }

        return null;
    }
}
