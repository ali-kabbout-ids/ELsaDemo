using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows;
using PurchaseOrderApi.Services;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Activities.ApplicationActivities
{
    [Activity("ApplicationFlow", "Saves Has Mane3 decision to DB")]
    public class SaveHasMane3DecisionActivity : CodeActivity
    {
        [Input] public Input<int> ApplicationId { get; set; } = default!;

        [Input] public Input<string> Decision { get; set; } = default!;

        [Input] public Input<string> Reason { get; set; } = default!;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
        {
            ApplicationService svc = ctx.GetRequiredService<ApplicationService>();
            int appId = ctx.Get(ApplicationId);
            string? decision = ctx.Get(Decision);
            string? reason = ctx.Get(Reason);

            ApplicationRequest? app = await svc.GetByIdAsync(appId);
            if (app != null)
            {
                app.HasMane3Decision = decision;
                app.HasMane3Reason = reason;
                app.UpdatedAt = DateTime.UtcNow;
                await svc.SaveAsync(app);
            }

            Console.WriteLine($"[FLOW] ▶  App #{appId} — HasMane3 saved: '{decision}'");
        }
    }
}
