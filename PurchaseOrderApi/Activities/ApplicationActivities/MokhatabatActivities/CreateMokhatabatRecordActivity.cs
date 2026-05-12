using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows;
using PurchaseOrderApi.Services;

namespace PurchaseOrderApi.Activities.ApplicationActivities.MokhatabatActivities
{
    [Activity("Mokhatabat", "Creates the Mokhatabat DB record")]
    public class CreateMokhatabatRecordActivity : CodeActivity
    {
        [Input] public Input<int> ApplicationId { get; set; } = default!;

        [Output] public Output<int>? MokhatabatId { get; set; }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
        {
            var svc = ctx.GetRequiredService<MokhatabatService>();
            int appId = ctx.Get(ApplicationId);

            var record = await svc.CreateAsync(appId);

            // Store the new row Id into the workflow variable
            ctx.Set(MokhatabatId, record.Id);

            Console.WriteLine($"[Mo5] ✅ Created Mo5atabatRequest #{record.Id} for App #{appId}");
        }
    }
}
