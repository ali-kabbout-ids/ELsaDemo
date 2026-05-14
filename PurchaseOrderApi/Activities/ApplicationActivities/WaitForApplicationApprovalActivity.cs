using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using PurchaseOrderApi.Dtos;
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
        Description = "Actions available to the assigned role for this step",
        UIHint = InputUIHints.CheckList,
        UIHandler = typeof(WorkflowActionUIProvider)
    )]
    public Input<string[]> AllowedActionKeys { get; set; } = default!;

    // ── Single clean output ───────────────────────────────────────────────────
    [Output(Description = "The result of this approval step")]
    public Output<ApprovalResult?>? Result { get; set; }

    // ── Execute ──────────────────────────────────────────────────────────────
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApprovalRole role = ctx.Get(RequiredRole);
        string? step = ctx.Get(StepName);

        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app is not null)
        {
            app.CurrentRequiredRole = role.ToString();
            app.CurrentStepName = step;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = appId.ToString(),
            Callback = OnResumedAsync,
            AutoBurn = true
        });

        Console.WriteLine($"[FLOW] ⏸  App #{appId} — '{step}' waiting for '{role}'");
    }

    // ── Resume ───────────────────────────────────────────────────────────────
    private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
    {
        IDictionary<string, object> input = ctx.WorkflowInput;
        string action = GetStr(input, "action") ?? "reject";
        string reason = GetStr(input, "reason") ?? string.Empty;

        var selectedKeys = ctx.Get(AllowedActionKeys) ?? [];

        // Resolve them to full WorkflowAction objects for your logic  
        var allowedActions = selectedKeys
            .Select(key => WorkflowActions.FindByKey(key))
            .Where(a => a != null)
            .ToList();

        // Collect extra fields — all as string, safely serializable
        Dictionary<string, string> extra = input
            .Where(kv => kv.Key is not "action" and not "reason")
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value?.ToString() ?? string.Empty);

        ApprovalResult result = new()
        {
            Action = action,
            Reason = reason,
            Extra = extra
        };

        // Persist against role
        ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
        int appId = ctx.Get(ApplicationId);
        ApprovalRole role = ctx.Get(RequiredRole);

        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app is not null)
        {
            switch (role)
            {
                case ApprovalRole.I3lamKanouni:
                    app.I3almKanouniDecision = action;
                    app.I3almKanouniReason = reason;
                    break;
                case ApprovalRole.Mo3awenCho3ba:
                    app.Mo3awenDecision = action;
                    app.Mo3awenReason = reason;
                    break;
                case ApprovalRole.FinalMo3awen:
                    app.FinalMo3awenDecision = action;
                    break;
            }
            app.CurrentRequiredRole = null;
            app.CurrentStepName = null;
            app.UpdatedAt = DateTime.UtcNow;
            await svc.SaveAsync(app);
        }

        ctx.Set(Result, result);

        Console.WriteLine($"[FLOW] ▶  App #{appId} — [{role}] action='{action}'");
        await ctx.CompleteActivityAsync();
    }
}