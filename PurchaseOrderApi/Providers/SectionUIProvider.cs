using Elsa.Workflows.UIHints.CheckList;
using PurchaseOrderApi.Enums;
using System.Reflection;

namespace PurchaseOrderApi.Providers
{
    public class SectionUIProvider : CheckListOptionsProviderBase
    {
        protected override ValueTask<ICollection<CheckListItem>> GetItemsAsync(
            PropertyInfo propertyInfo,
            object? context,
            CancellationToken cancellationToken = default)
        {
            ICollection<CheckListItem> items = WorkflowSections.All
                .Select(s => new CheckListItem(s.DisplayName, s.Key))
                .ToList();

            return ValueTask.FromResult(items);
        }
    }
}
