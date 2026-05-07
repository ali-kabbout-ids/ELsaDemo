using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;

namespace PurchaseOrderApi.Activities;

[Activity("Demo", "Marks the order as rejected")]
public class RejectOrderActivity : CodeActivity
{
    [Input] public Input<int>    OrderId { get; set; } = default!;
    [Input] public Input<string> Reason  { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        var store   = ctx.GetRequiredService<PurchaseOrderService>();
        var orderId = ctx.Get(OrderId);
        var reason  = ctx.Get(Reason);
        var order   = await store.GetByIdAsync(orderId)
            ?? throw new InvalidOperationException($"PO #{orderId} not found.");

        order.Status          = OrderStatus.Rejected;
        order.RejectionReason = reason;
        await store.SaveAsync(order);
        Console.WriteLine($"[ELSA] RejectOrder ❌  PO #{orderId} REJECTED. Reason: {reason}");
    }
}
