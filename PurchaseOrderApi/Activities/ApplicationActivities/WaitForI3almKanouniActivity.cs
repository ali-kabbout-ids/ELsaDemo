using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;
using static PurchaseOrderApi.Helpers.StringHelper;

namespace PurchaseOrderApi.Activities;

/// <summary>
/// STEP 1 — I3alm Kanouni (Legal Advisor) Review.
/// Suspends the workflow. Resumes when POST /api/applications/{id}/i3lam-kanouni/decide is called.
/// Role: I3lam Kanouni
/// </summary>
[Activity("ApplicationFlow", "Suspends and waits for i3lam Kanouni (Legal Advisor) review decision")]
public class WaitForI3almKanouniActivity : Activity
{
    [Input(Description = "Application ID")]
    public Input<int> ApplicationId { get; set; } = default!;

    [Output(Description = "Decision: approved | rejected")]
    public Output<string>? Decision { get; set; }

    [Output(Description = "Reviewer reason / notes")]
    public Output<string>? Reason { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.Status = ApplicationStatus.PendingI3almKanouniReview;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        string stimulus = appId.ToString();

        ctx.CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = stimulus,
            Callback = OnResumedAsync,
            AutoBurn = true
        });

        Console.WriteLine($"[FLOW] ⏸  App #{appId} → Round {app?.ReviewRound} — Waiting for I3alm Kanouni review.");
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        IDictionary<string, object> input = ctx.WorkflowInput;
        string decision = GetStr(input, "decision") ?? "rejected";
        string reason = GetStr(input, "reason") ?? string.Empty;

        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.I3almKanouniDecision = decision;
            app.I3almKanouniReason = reason;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.Set(Decision, decision);
        ctx.Set(Reason, reason);

        Console.WriteLine($"[FLOW] ▶  App #{appId} — I3alm Kanouni: '{decision}' ({reason})");
        await ctx.CompleteActivityAsync();
    }
}