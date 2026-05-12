using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows;
using PurchaseOrderApi.Enums;
using static PurchaseOrderApi.Helpers.StringHelper;
using PurchaseOrderApi.Services;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Activities.ApplicationActivities.MokhatabatActivities
{
    [Activity("Mokhatabat", "Step 1 — first officer review")]
    public class MokhatabatStep1Activity : Activity
    {
        [Input] public Input<int> ApplicationId { get; set; } = default!;

        [Input] public Input<int> MokhatabatId { get; set; } = default!;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
        {
            MokhatabatService svc = ctx.GetRequiredService<MokhatabatService>();
            int mo5Id = ctx.Get(MokhatabatId);
            Models.MokhatabatRequest? record = await svc.GetByIdAsync(mo5Id);
            if (record != null)
            {
                record.Status = MokhatabatStatus.PendingStep1;
                record.UpdatedAt = DateTime.UtcNow;
                await svc.SaveAsync(record);
            }
            ctx.CreateBookmark(new CreateBookmarkArgs
            {
                Stimulus = ctx.Get(ApplicationId).ToString(),
                Callback = OnResumedAsync,
                AutoBurn = true
            });
            Console.WriteLine($"[Mo5] ⏸  Mo5 #{mo5Id} — Step 1 waiting.");
        }

        private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
        {
            IDictionary<string, object> input = ctx.WorkflowInput;
            string decision = GetStr(input, "decision") ?? "approved";
            string reason = GetStr(input, "reason") ?? string.Empty;

            MokhatabatService svc = ctx.GetRequiredService<MokhatabatService>();
            int mokhId = ctx.Get(MokhatabatId);
            MokhatabatRequest? record = await svc.GetByIdAsync(mokhId);
            if (record != null)
            {
                record.Step1Decision = decision;
                record.Step1Reason = reason;
                record.Step1DecidedAt = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;
                await svc.SaveAsync(record);
            }
            Console.WriteLine($"[Mo5] ▶  Mo5 #{mokhId} — Step 1: '{decision}'");
            await ctx.CompleteActivityAsync();
        }
    }
}
