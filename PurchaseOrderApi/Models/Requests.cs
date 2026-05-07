namespace PurchaseOrderApi.Models;

public record CreateOrderRequest(
    string  Title,
    decimal Amount,
    string  RequesterEmail,
    string  ManagerEmail);

public record ApprovalDecisionRequest(
    string  Decision,    // "approved" | "rejected"
    string  Reason = "");
