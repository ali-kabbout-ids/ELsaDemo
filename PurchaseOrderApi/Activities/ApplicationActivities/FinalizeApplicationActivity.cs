using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;

namespace PurchaseOrderApi.Activities;

/// <summary>
/// Final activity — marks the application as Approved or Rejected in the DB.
/// Two instances are used in the workflow: one for each outcome branch.
/// </summary>
[Activity("ApplicationFlow", "Finalizes the application with its outcome")]
public class FinalizeApplicationActivity : CodeActivity
{
    [Input(Description = "Application ID")]
    public Input<int> ApplicationId { get; set; } = default!;

    /// <summary>True = Approved, False = Rejected</summary>
    [Input(Description = "True = Approved, False = Rejected")]
    public Input<bool> IsApproved { get; set; } = default!;

    [Input(Description = "Rejection reason (if rejected)")]
    public Input<string>? RejectionReason { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        bool isApproved = ctx.Get(IsApproved);
        string? reason = RejectionReason != null ? ctx.Get(RejectionReason) : null;

        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app != null)
        {
            app.Status = isApproved ? ApplicationStatus.Approved : ApplicationStatus.Rejected;
            app.RejectionReason = isApproved ? null : reason;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        string emoji = isApproved ? "✅" : "❌";
        Console.WriteLine($"[FLOW] {emoji}  App #{appId} — {(isApproved ? "APPROVED" : $"REJECTED: {reason}")}");
    }
}