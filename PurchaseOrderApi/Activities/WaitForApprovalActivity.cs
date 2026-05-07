using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace PurchaseOrderApi.Activities;

/// ELSA CORE FEATURE: Bookmark — Suspend / Resume
/// This activity suspends the workflow by creating a Bookmark.
/// No thread is blocked. The HTTP request returns immediately.
/// When ResumeWorkflowAsync is called, OnResumedAsync fires.
[Activity("Demo", "Suspends and waits for manager approval")]
public class WaitForApprovalActivity : Activity   // NOTE: Activity not CodeActivity
{
    [Output] public Output<string>? Decision { get; set; }
    [Output] public Output<string>? Reason   { get; set; }

    protected override ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        ctx.CreateBookmark(new CreateBookmarkArgs
        {
            Callback                  = OnResumedAsync,
            IncludeActivityInstanceId = true
        });

        Console.WriteLine("[ELSA] WaitForApproval ⏸  Workflow SUSPENDED. Bookmark created.");
        return ValueTask.CompletedTask;
        // DO NOT call CompleteActivityAsync here — called inside OnResumedAsync
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        var input    = ctx.WorkflowExecutionContext.Input ?? new Dictionary<string, object>();
        var decision = GetStr(input, "decision") ?? "rejected";
        var reason   = GetStr(input, "reason")   ?? string.Empty;

        ctx.Set(Decision, decision);
        ctx.Set(Reason,   reason);

        Console.WriteLine($"[ELSA] WaitForApproval ▶  RESUMED. Decision='{decision}' Reason='{reason}'");
        await ctx.CompleteActivityAsync();
    }

    private static string? GetStr(IDictionary<string, object> d, string k)
        => d.TryGetValue(k, out var v) ? v?.ToString() : null;
}
