using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Providers;
using PurchaseOrderApi.Services;
using static PurchaseOrderApi.Helpers.StringHelper;

namespace PurchaseOrderApi.Activities;

[Activity("ApplicationFlow", "Suspends and waits for a role-based human approval")]
public class WaitForApplicationApprovalActivity : Activity
{
    [Input(
        Description = "Role required to submit a decision for this step",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(ApprovalRoleSelectListProvider)
    )]
    public Input<ApprovalRole> RequiredRole { get; set; } = default!;

    [Input(Description = "Human-readable step name shown in UI")]
    public Input<string> StepName { get; set; } = default!;

    [Input(Description = "Application ID")]
    public Input<int> ApplicationId { get; set; } = default!;

    [Input(
        Description = "Status to set while this step is pending",
        UIHint = InputUIHints.DropDown
    )]
    public Input<ApplicationStatus> PendingStatus { get; set; } = default!;

    [Input(
        Description = "Status to set after decision is saved",
        UIHint = InputUIHints.DropDown
    )]
    public Input<ApplicationStatus> CompletedStatus { get; set; } = default!;

    [Output] public Output<string>? Decision { get; set; }
    [Output] public Output<string>? Reason { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApprovalRole requiredRole = ctx.Get(RequiredRole);
        string? stepName = ctx.Get(StepName);
        ApplicationStatus pendingStatus = ctx.Get(PendingStatus);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);

        if (app != null)
        {
            app.Status = pendingStatus;
            app.CurrentRequiredRole = requiredRole.ToString();
            app.CurrentStepName = stepName;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = appId.ToString(),
            Callback = OnResumedAsync,
            AutoBurn = true
        });

        Console.WriteLine($"[FLOW] ⏸  App #{appId} — '{stepName}' waiting for '{requiredRole}'");
    }

    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        IDictionary<string, object> input = ctx.WorkflowInput;
        string decision = GetStr(input, "decision") ?? "rejected";
        string reason = GetStr(input, "reason") ?? string.Empty;
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApprovalRole role = ctx.Get(RequiredRole);
        ApplicationStatus completedStatus = ctx.Get(CompletedStatus);
        ApplicationRequest? app = await svc.GetByIdAsync(appId);

        if (app != null)
        {
            // ── Save decision to the correct fields based on role ─────────
            switch (role)
            {
                case ApprovalRole.I3lamKanouni:
                    app.I3almKanouniDecision = decision;
                    app.I3almKanouniReason = reason;
                    break;
                case ApprovalRole.Mo3awenCho3ba:
                    app.Mo3awenDecision = decision;
                    app.Mo3awenReason = reason;
                    break;
                case ApprovalRole.FinalMo3awen:
                    app.FinalMo3awenDecision = decision;
                    break;
            }

            app.Status = completedStatus;
            app.CurrentRequiredRole = null;
            app.CurrentStepName = null;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.Set(Decision, decision);
        ctx.Set(Reason, reason);

        Console.WriteLine($"[FLOW] ▶  App #{appId} — [{role}] '{decision}'. Status → {completedStatus}");
        await ctx.CompleteActivityAsync();
    }
}