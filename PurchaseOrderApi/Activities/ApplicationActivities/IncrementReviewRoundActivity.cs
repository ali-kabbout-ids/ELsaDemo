using Elsa.Extensions;
using Elsa.Workflows;
using PurchaseOrderApi.Services;
using Elsa.Workflows.Attributes;

namespace PurchaseOrderApi.Activities.ApplicationActivities
{
    [Activity("ApplicationFlow")]
    public class IncrementReviewRoundActivity : CodeActivity
    {
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            ApplicationService svc = context.GetRequiredService<ApplicationService>();

            int appId = context.GetVariable<int>("ApplicationId");

            Models.ApplicationRequest? app = await svc.GetByIdAsync(appId);

            if (app != null)
            {
                app.ReviewRound++;

                await svc.SaveAsync(app);
            }
        }
    }
}
