using PurchaseOrderApi.Enums;
using System.Reflection;
using Elsa.Workflows.UIHints.Dropdown;

namespace PurchaseOrderApi.Providers
{
    public class ApprovalRoleSelectListProvider : DropDownOptionsProviderBase
    {
        protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(
            PropertyInfo propertyInfo,
            object? context,
            CancellationToken cancellationToken)
        {
            var items = Enum.GetValues<ApprovalRole>()
                .Select(r => new SelectListItem(r.ToString(), r.ToString())) 
                .ToList();

            return ValueTask.FromResult<ICollection<SelectListItem>>(items);
        }
    }
}
