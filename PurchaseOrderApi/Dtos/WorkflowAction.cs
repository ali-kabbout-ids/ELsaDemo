namespace PurchaseOrderApi.Dtos
{
    public record WorkflowAction
    {
        /// <summary>Machine key sent to the API — e.g. "approve"</summary>
        public string Key { get; init; } = default!;

        /// <summary>Human label rendered as button text — e.g. "Approve"</summary>
        public string Label { get; init; } = default!;

        /// <summary>Button style hint for UI — primary | danger | warning | default</summary>
        public string Style { get; init; } = "default";

        /// <summary>Whether this action requires a reason/comment</summary>
        public bool RequiresReason { get; init; } = false;

        public IReadOnlyList<ActionField> ExtraFields { get; init; }
            = Array.Empty<ActionField>();
    }
}
