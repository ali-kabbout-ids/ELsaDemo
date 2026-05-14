namespace PurchaseOrderApi.Dtos
{
    public record ApprovalResult
    {
        public string Action { get; init; } = string.Empty;

        public string Reason { get; init; } = string.Empty;

        public Dictionary<string, string> Extra { get; init; } = new();

        public bool IsApproved => Action == "approve";

        public bool IsRejected => Action == "reject";

        public bool GetBool(string key, bool fallback = false)
            => Extra.TryGetValue(key, out string? v)
                ? v is "true" or "1" or "yes"
                : fallback;

        public string? GetString(string key)
            => Extra.TryGetValue(key, out string? v) ? v : null;
    }
}
