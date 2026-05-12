using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;
using static PurchaseOrderApi.Helpers.StringHelper;

namespace PurchaseOrderApi.Activities;

/// <summary>
/// STEP 2 — Mo3awen l Cho3ba (Dept Assistant) Review Application.
/// Suspends the workflow. Resumes when POST /api/applications/{id}/mo3awen/decide is called.
/// Role: Mo3awen l Cho3ba
/// </summary>
[Activity("ApplicationFlow", "Suspends and waits for Mo3awen l Cho3ba (Dept Assistant) review decision")]
public class WaitForMo3awenCho3baActivity : Activity
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
            app.Status = ApplicationStatus.PendingMo3awenReview;
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

        Console.WriteLine($"[FLOW] ⏸  App #{appId} → Round {app?.ReviewRound} — Waiting for Mo3awen l Cho3ba review.");
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        IDictionary<string, object> input = ctx.WorkflowExecutionContext.Input ?? new Dictionary<string, object>();
        string decision = GetStr(input, "decision") ?? "rejected";
        string reason = GetStr(input, "reason") ?? string.Empty;

        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.Mo3awenDecision = decision;
            app.Mo3awenReason = reason;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.Set(Decision, decision);
        ctx.Set(Reason, reason);

        Console.WriteLine($"[FLOW] ▶  App #{appId} — Mo3awen l Cho3ba: '{decision}' ({reason})");
        await ctx.CompleteActivityAsync();
    }
}