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
        var store   = ctx.GetRequiredService<PurchaseOrderStore>();
        var orderId = ctx.Get(OrderId);
        var order   = store.GetById(orderId)!;
        order.Status = OrderStatus.Approved;
        store.Save(order);
        Console.WriteLine($"[ELSA] ApproveOrder ✅  PO #{orderId} APPROVED");
    }
}
