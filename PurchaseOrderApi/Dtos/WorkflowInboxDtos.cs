namespace PurchaseOrderApi.Dtos;

// Item shown in the inbox list
public sealed record InboxItemDto(
    string WorkflowInstanceId,
    string BookmarkId,
    int ApplicationId,
    string StepName,
    string RequiredRole,
    ICollection<WorkflowAction> AvailableActions
);

// Request body for submitting action
public record SubmitActionRequest(
    string Action,
    string? Reason,
    Dictionary<string, object>? Extra
);

