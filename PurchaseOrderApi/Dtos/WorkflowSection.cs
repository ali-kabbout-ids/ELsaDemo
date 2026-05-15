namespace PurchaseOrderApi.Dtos
{
    public sealed class WorkflowSection
    {
        public string Key { get; init; } = default!;

        public string DisplayName { get; init; } = default!;

        public string Description { get; init; } = string.Empty;
    }
}
