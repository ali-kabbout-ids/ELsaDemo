using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Services;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Activities.ApplicationActivities
{
    [Activity("ApplicationFlow", "Sets application status to PendingHasMane3Check")]
    public class SetHasMane3PendingActivity : CodeActivity
    {
        [Input] public Input<int> ApplicationId { get; set; } = default!;

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
            Console.WriteLine($"[FLOW] ⏸  App #{appId} — Waiting for Has Mane3 HTTP input.");
        }
    }
}
