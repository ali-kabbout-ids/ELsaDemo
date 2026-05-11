using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;
using static PurchaseOrderApi.Helpers.StringHelper;

namespace PurchaseOrderApi.Activities;

/// <summary>
/// STEP 4b — Mokhatabat Sub-Workflow (Correspondence processing).
///
/// In a real system this would invoke a separate Mokhatabat workflow via InvokeWorkflow activity.
/// For PoC, it's modelled as a single suspend/resume bookmark that represents the sub-workflow
/// completing and reporting back.
///
/// Resumes when POST /api/applications/{id}/mo5atabat/decide is called.
/// </summary>
[Activity("ApplicationFlow", "Suspends while the Mokhatabat (Correspondence) sub-workflow runs")]
public class WaitForMo5atabatActivity : Activity
{
    [Input(Description = "Application ID")]
    public Input<int> ApplicationId { get; set; } = default!;

    [Output(Description = "Sub-workflow outcome: completed | rejected")]
    public Output<string>? Decision { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.Status = ApplicationStatus.PendingMo5atabat;
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

        Console.WriteLine($"[FLOW] ⏸  App #{appId} — Mokhatabat sub-workflow started. Waiting for completion.");
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        IDictionary<string, object> input = ctx.WorkflowExecutionContext.Input ?? new Dictionary<string, object>();
        string decision = GetStr(input, "decision") ?? "completed";

        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.Mo5atabatDecision = decision;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.Set(Decision, decision);

        Console.WriteLine($"[FLOW] ▶  App #{appId} — Mokhatabat sub-workflow: '{decision}'");
        await ctx.CompleteActivityAsync();
    }
}