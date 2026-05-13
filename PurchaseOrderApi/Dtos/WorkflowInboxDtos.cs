namespace PurchaseOrderApi.Dtos;

// Item shown in the inbox list
public sealed record InboxItemDto(
    string WorkflowInstanceId,
    string BookmarkId,
    int ApplicationId,
    string StepName,
    string RequiredRole
);

// Request body for submitting a decision
public sealed record SubmitDecisionRequest(string Decision, string Reason);

