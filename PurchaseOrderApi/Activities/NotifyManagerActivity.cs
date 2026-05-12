using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace PurchaseOrderApi.Activities;

[Activity("Demo", "Notifies the manager to approve the order")]
public class NotifyManagerActivity : CodeActivity
{
    [Input] public Input<int>    OrderId      { get; set; } = default!;
    [Input] public Input<string> ManagerEmail { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext ctx)
    {
        int id    = ctx.Get(OrderId);
        string? email = ctx.Get(ManagerEmail);

        Console.WriteLine($"[ELSA] NotifyManager  📧  Sent to '{email}' for PO #{id}");
        Console.WriteLine($"[ELSA]   → To approve: POST /api/orders/{id}/decide {{\"decision\":\"approved\"}}");
        Console.WriteLine($"[ELSA]   → To reject:  POST /api/orders/{id}/decide {{\"decision\":\"rejected\",\"reason\":\"...\"}}");
    }
}
