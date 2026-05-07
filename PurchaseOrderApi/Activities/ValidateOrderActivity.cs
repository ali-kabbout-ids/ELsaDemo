using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;

namespace PurchaseOrderApi.Activities;

[Activity("Demo", "Validates the purchase order")]
public class ValidateOrderActivity : CodeActivity
{
    [Input(Description = "The PO ID to validate")]
    public Input<int> OrderId { get; set; } = default!;

    [Output(Description = "Manager email for notification")]
    public Output<string>? ManagerEmail { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        var store   = ctx.GetRequiredService<PurchaseOrderStore>();
        var orderId = ctx.Get(OrderId);

        var order = store.GetById(orderId)
            ?? throw new InvalidOperationException($"PO #{orderId} not found.");

        if (order.Amount <= 0)
            throw new InvalidOperationException($"PO #{orderId}: Amount must be > 0.");

        if (string.IsNullOrWhiteSpace(order.ManagerEmail))
            throw new InvalidOperationException($"PO #{orderId}: ManagerEmail is required.");

        order.Status = OrderStatus.PendingApproval;
        store.Save(order);

        ctx.Set(ManagerEmail, order.ManagerEmail);
        Console.WriteLine($"[ELSA] ValidateOrder ✓  PO #{orderId}  Amount={order.Amount:C}  Manager={order.ManagerEmail}");
    }
}
