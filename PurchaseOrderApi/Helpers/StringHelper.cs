namespace PurchaseOrderApi.Helpers
{
    public static class StringHelper
    {
        public static string? GetStr(IDictionary<string, object> d, string k)
        {
            return d.TryGetValue(k, out object? v) ? v?.ToString() : null;
        }
    }
}
