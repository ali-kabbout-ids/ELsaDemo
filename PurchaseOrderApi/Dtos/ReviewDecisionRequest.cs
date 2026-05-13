namespace PurchaseOrderApi.Dtos
{
    public record ReviewDecisionRequest(
        string Decision,           // "approved" | "rejected" | "completed" | "no_obstacle" | "has_obstacle"
        string UserRole,
        string Reason = "");
}
