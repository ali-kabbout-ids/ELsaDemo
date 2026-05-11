using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;
using static PurchaseOrderApi.Helpers.StringHelper;

namespace PurchaseOrderApi.Activities;

/// <summary>
/// STEP 6 — Has Mane3 Anouni (Legal Obstacle Check).
/// A legal officer checks whether there is a legal obstacle that blocks the application.
///
/// Decision values:
///   "no_obstacle"  → application proceeds to Approved
///   "has_obstacle" → application goes to Rejected
///
/// Resumes when POST /api/applications/{id}/has-mane3/decide is called.
/// </summary>
[Activity("ApplicationFlow", "Suspends and waits for Has Mane3 Anouni (Legal Obstacle) check result")]
public class WaitForHasMane3AnouniActivity : Activity
{
    [Input(Description = "Application ID")]
    public Input<int> ApplicationId { get; set; } = default!;

    [Output(Description = "Result: no_obstacle | has_obstacle")]
    public Output<string>? Decision { get; set; }

    [Output(Description = "Obstacle reason (if any)")]
    public Output<string>? Reason { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.Status = ApplicationStatus.PendingHasMane3Check;
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

        Console.WriteLine($"[FLOW] ⏸  App #{appId} — Waiting for Has Mane3 Anouni check.");
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        IDictionary<string, object> input = ctx.WorkflowExecutionContext.Input ?? new Dictionary<string, object>();
        string decision = GetStr(input, "decision") ?? "no_obstacle";
        string reason = GetStr(input, "reason") ?? string.Empty;

        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.HasMane3Decision = decision;
            app.HasMane3Reason = reason;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.Set(Decision, decision);
        ctx.Set(Reason, reason);

        Console.WriteLine($"[FLOW] ▶  App #{appId} — Has Mane3: '{decision}' ({reason})");
        await ctx.CompleteActivityAsync();
    }
}