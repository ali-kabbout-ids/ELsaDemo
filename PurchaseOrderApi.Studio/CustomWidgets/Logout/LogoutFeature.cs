using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;

namespace PurchaseOrderApi.Studio.CustomWidgets.Logout
{
    public class LogoutFeature(IAppBarService appBarService) : FeatureBase
    {
        public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            appBarService.AddComponent<LogoutButton>(order: 100);
            return base.InitializeAsync(cancellationToken);
        }
    }
}
