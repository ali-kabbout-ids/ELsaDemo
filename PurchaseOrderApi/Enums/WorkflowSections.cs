using PurchaseOrderApi.Dtos;
using System.Reflection;

namespace PurchaseOrderApi.Enums
{
    public static class WorkflowSections
    {
        public static readonly WorkflowSection General = new()
        {
            Key = "General",
            DisplayName = "General Info",
            Description = "Basic application details"
        };

        public static readonly WorkflowSection Info = new()
        {
            Key = "Info",
            DisplayName = "Additional Info",
            Description = "Extra information fields"
        };

        public static readonly WorkflowSection Attachments = new()
        {
            Key = "Attachments",
            DisplayName = "Attachments",
            Description = "Uploaded files and documents"
        };

        public static readonly WorkflowSection History = new()
        {
            Key = "History",
            DisplayName = "Review History",
            Description = "Past decisions and rounds"
        };

        public static IReadOnlyList<WorkflowSection> All { get; } =
            typeof(WorkflowSections)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(WorkflowSection))
                .Select(f => (WorkflowSection)f.GetValue(null)!)
                .GroupBy(s => s.Key)
                .Select(g => g.First())
                .ToList();

        private static readonly Dictionary<string, WorkflowSection> _byKey =
            All.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

        public static WorkflowSection? FindByKey(string key)
            => _byKey.TryGetValue(key, out WorkflowSection? section) ? section : null;

        public static bool IsValid(string key) => _byKey.ContainsKey(key);
    }
}
