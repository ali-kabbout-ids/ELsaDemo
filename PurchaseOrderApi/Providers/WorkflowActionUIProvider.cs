using Elsa.Workflows.UIHints.CheckList;
using PurchaseOrderApi.Enums;
using System.Reflection;

namespace PurchaseOrderApi.Providers
{
    public class WorkflowActionUIProvider : CheckListOptionsProviderBase
    {
        protected override ValueTask<ICollection<CheckListItem>> GetItemsAsync(
            PropertyInfo propertyInfo,
            object? context,
            CancellationToken cancellationToken = default)
        {
            ICollection<CheckListItem> items = WorkflowActions.All
                .Select(a => new CheckListItem(a.Label, a.Key))
                .ToList();

            return ValueTask.FromResult(items);
        }
    }
}
