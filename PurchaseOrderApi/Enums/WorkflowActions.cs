using PurchaseOrderApi.Dtos;
using System.Reflection;

namespace PurchaseOrderApi.Enums
{
    public static class WorkflowActions
    {
        public static readonly WorkflowAction Approve = new()
        {
            Key = "approve",
            Label = "Approve",
            Style = "primary",
            RequiresReason = false
        };

        public static readonly WorkflowAction ApproveWithMo5atabat = new()
        {
            Key = "approveWithMo5atabat",
            Label = "Approve with mo5atabat",
            Style = "primary",
            RequiresReason = false,
            ExtraFields = [
                new ActionField
                {
                    Key        = "requiresMo5atabat",
                    Label      = "Requires Mokhatabat?",
                    FieldType  = "checkbox",
                    IsRequired = false
                }
            ]
        };

        public static readonly WorkflowAction Reject = new()
        {
            Key = "reject",
            Label = "Reject",
            Style = "danger",
            RequiresReason = true
        };

        public static readonly WorkflowAction RequestInfo = new()
        {
            Key = "request_info",
            Label = "Request More Info",
            Style = "warning",
            RequiresReason = true
        };

        public static readonly WorkflowAction Acknowledge = new()
        {
            Key = "acknowledge",
            Label = "Acknowledge",
            Style = "default",
            RequiresReason = false
        };

        public static IReadOnlyList<WorkflowAction> All { get; } =
            typeof(WorkflowActions)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(WorkflowAction))
                .Select(f => (WorkflowAction)f.GetValue(null)!)
                .GroupBy(a => a.Key)            
                .Select(g => g.First())
                .OrderBy(a => a.Label)
                .ToList();

        private static readonly Dictionary<string, WorkflowAction> _byKey =
            All.ToDictionary(a => a.Key, StringComparer.OrdinalIgnoreCase);

        public static WorkflowAction? FindByKey(string key)
            => _byKey.TryGetValue(key, out WorkflowAction? action) ? action : null;

        public static bool IsValid(string key) => _byKey.ContainsKey(key);
    }
}
