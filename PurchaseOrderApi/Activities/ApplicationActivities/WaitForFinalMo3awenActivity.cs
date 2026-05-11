using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;
using static PurchaseOrderApi.Helpers.StringHelper;

namespace PurchaseOrderApi.Activities;

/// <summary>
/// STEP 5 — Final Mo3awen l Cho3ba sign-off after Mo5atabat path merges.
/// This is distinct from STEP 2 — it represents the final notification/action
/// by Mo3awen before the Has Mane3 Anouni check.
///
/// Resumes when POST /api/applications/{id}/final-mo3awen/decide is called.
/// </summary>
[Activity("ApplicationFlow", "Suspends and waits for final Mo3awen l Cho3ba sign-off")]
public class WaitForFinalMo3awenActivity : Activity
{
    [Input(Description = "Application ID")]
    public Input<int> ApplicationId { get; set; } = default!;

    [Output(Description = "Decision: approved | rejected")]
    public Output<string>? Decision { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.Status = ApplicationStatus.PendingFinalMo3awenReview;
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

        Console.WriteLine($"[FLOW] ⏸  App #{appId} — Waiting for final Mo3awen l Cho3ba sign-off.");
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        IDictionary<string, object> input = ctx.WorkflowExecutionContext.Input ?? new Dictionary<string, object>();
        string decision = GetStr(input, "decision") ?? "approved";

        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.FinalMo3awenDecision = decision;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.Set(Decision, decision);

        Console.WriteLine($"[FLOW] ▶  App #{appId} — Final Mo3awen: '{decision}'");
        await ctx.CompleteActivityAsync();
    }
}