namespace PurchaseOrderApi.Helpers
{
    public static class DictionaryHelper
    {
        public static int TryGetInt(IDictionary<string, object>? d, string key)
            => d != null && d.TryGetValue(key, out object? v) ? Convert.ToInt32(v) : 0;

        public static bool TryGetBool(IDictionary<string, object>? d, string key)
            => d != null &&
               d.TryGetValue(key, out object? v) &&
               (v is bool b ? b : v?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
    }
}
