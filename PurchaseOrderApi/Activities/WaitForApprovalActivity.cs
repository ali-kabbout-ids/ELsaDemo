using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using static PurchaseOrderApi.Helpers.StringHelper;

namespace PurchaseOrderApi.Activities;

/// ELSA CORE FEATURE: Bookmark — Suspend / Resume
/// This activity suspends the workflow by creating a Bookmark.
/// No thread is blocked. The HTTP request returns immediately.
/// When ResumeWorkflowAsync is called, OnResumedAsync fires.
[Activity("Demo", "Suspends and waits for manager approval")]
public class WaitForApprovalActivity : Activity 
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
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        IDictionary<string, object> input    = ctx.WorkflowExecutionContext.Input ?? new Dictionary<string, object>();
        string decision = GetStr(input, "decision") ?? "rejected";
        string reason   = GetStr(input, "reason")   ?? string.Empty;

        ctx.Set(Decision, decision);
        ctx.Set(Reason,   reason);

        Console.WriteLine($"[ELSA] WaitForApproval ▶  RESUMED. Decision='{decision}' Reason='{reason}'");
        await ctx.CompleteActivityAsync();
    }
}
