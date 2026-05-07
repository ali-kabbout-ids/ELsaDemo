using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;

namespace PurchaseOrderApi.Activities;

[Activity("Demo", "Marks the order as approved")]
public class ApproveOrderActivity : CodeActivity
{
    [Input] public Input<int> OrderId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        var store   = ctx.GetRequiredService<PurchaseOrderService>();
        int orderId = ctx.Get(OrderId);
        PurchaseOrder order   = await store.GetByIdAsync(orderId)
            ?? throw new InvalidOperationException($"PO #{orderId} not found.");

        order.Status = OrderStatus.Approved;
        await store.SaveAsync(order);
        Console.WriteLine($"[ELSA] ApproveOrder ✅  PO #{orderId} APPROVED");
    }
}
