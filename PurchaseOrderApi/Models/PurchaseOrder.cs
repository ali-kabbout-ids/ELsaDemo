namespace PurchaseOrderApi.Models;

public class PurchaseOrder
{
    public int         Id                 { get; set; }
    public string      Title              { get; set; } = string.Empty;
    public decimal     Amount             { get; set; }
    public string      RequesterEmail     { get; set; } = string.Empty;
    public string      ManagerEmail       { get; set; } = string.Empty;
    public OrderStatus Status             { get; set; } = OrderStatus.Draft;
    public string?     RejectionReason    { get; set; }
    public DateTime    CreatedAt          { get; set; } = DateTime.UtcNow;
    public string?     WorkflowInstanceId { get; set; }
}

public enum OrderStatus
{
    Draft, PendingApproval, Approved, Rejected
}
