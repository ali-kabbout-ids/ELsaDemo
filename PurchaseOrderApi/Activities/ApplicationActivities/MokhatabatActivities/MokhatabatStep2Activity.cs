using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows;
using static PurchaseOrderApi.Helpers.StringHelper;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Services;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Activities.ApplicationActivities.MokhatabatActivities
{
    [Activity("Mokhatabat", "Step 2 — second officer review")]
    public class MokhatabatStep2Activity : Activity
    {
        [Input] public Input<int> ApplicationId { get; set; } = default!;

        [Input] public Input<int> MokhatabatId { get; set; } = default!;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
        {
            MokhatabatService svc = ctx.GetRequiredService<MokhatabatService>();
            int mo5Id = ctx.Get(MokhatabatId);
            MokhatabatRequest? record = await svc.GetByIdAsync(mo5Id);
            if (record != null)
            {
                record.Status = MokhatabatStatus.PendingStep2;
                record.UpdatedAt = DateTime.UtcNow;
                await svc.SaveAsync(record);
            }
            ctx.CreateBookmark(new CreateBookmarkArgs
            {
                Stimulus = ctx.Get(ApplicationId).ToString(),
                Callback = OnResumedAsync,
                AutoBurn = true
            });
            Console.WriteLine($"[Mo5] ⏸  Mo5 #{mo5Id} — Step 2 waiting.");
        }

        private async ValueTask OnResumedAsync(ActivityExecutionContext ctx)
        {
            IDictionary<string, object> input = ctx.WorkflowInput;
            string decision = GetStr(input, "decision") ?? "completed";
            string reason = GetStr(input, "reason") ?? string.Empty;

            MokhatabatService svc = ctx.GetRequiredService<MokhatabatService>();
            int mo5Id = ctx.Get(MokhatabatId);
            Models.MokhatabatRequest? record = await svc.GetByIdAsync(mo5Id);
            if (record != null)
            {
                record.Step2Decision = decision;
                record.Step2Reason = reason;
                record.Step2DecidedAt = DateTime.UtcNow;
                record.Status = MokhatabatStatus.Completed;
                record.UpdatedAt = DateTime.UtcNow;
                await svc.SaveAsync(record);
            }
            Console.WriteLine($"[Mo5] ▶  Mo5 #{mo5Id} — Step 2: '{decision}'");
            await ctx.CompleteActivityAsync();
        }
    }
}
