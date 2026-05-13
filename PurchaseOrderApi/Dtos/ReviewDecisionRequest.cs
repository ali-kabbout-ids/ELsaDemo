namespace PurchaseOrderApi.Dtos
{
    public record ReviewDecisionRequest(
        string Decision,           // "approved" | "rejected" | "completed" | "no_obstacle" | "has_obstacle"
        bool RequiresMo5atabat,
        string Reason = "");
}
