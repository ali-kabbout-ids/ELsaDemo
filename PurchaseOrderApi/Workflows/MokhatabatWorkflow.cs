using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Activities.ApplicationActivities.MokhatabatActivities;
using PurchaseOrderApi.Helpers;
using Elsa.Workflows.Activities.Flowchart.Models;
using Endpoint = Elsa.Workflows.Activities.Flowchart.Models.Endpoint;
using static PurchaseOrderApi.Helpers.DictionaryHelper;
using Elsa.Workflows.Memory;

namespace PurchaseOrderApi.Workflows
{
    public class MokhatabatWorkflow : WorkflowBase
    {
        public static string DefinitionId => nameof(MokhatabatWorkflow);

        protected override void Build(IWorkflowBuilder builder)
        {
            Variable<int> appIdVar = builder.WithVariable<int>("ApplicationId", 0).WithWorkflowStorage();
            Variable<int> mokhtabatIdVar = builder.WithVariable<int>("MokhatabatId", 0).WithWorkflowStorage();

            SetVariable<int> setAppId = new SetVariable<int>
            {
                Id = "Mo5SetAppId",
                Variable = appIdVar,
                Value = new Input<int>(ctx =>
                    TryGetInt(ctx.GetWorkflowExecutionContext().Input, "applicationId"))
            }.WithLayout(x: 50, y: 100);

            CreateMokhatabatRecordActivity createRecord = new CreateMokhatabatRecordActivity
            {
                Id = "CreateMo5Record",
                ApplicationId = new Input<int>(appIdVar),
                MokhatabatId = new Output<int>(mokhtabatIdVar)
            }.WithLayout(x: 300, y: 100);

            MokhatabatStep1Activity step1 = new MokhatabatStep1Activity
            {
                Id = "Mo5Step1",
                ApplicationId = new Input<int>(appIdVar),
                MokhatabatId = new Input<int>(mokhtabatIdVar)
            }.WithLayout(x: 580, y: 100, w: 220);

            MokhatabatStep2Activity step2 = new MokhatabatStep2Activity
            {
                Id = "Mo5Step2",
                ApplicationId = new Input<int>(appIdVar),
                MokhatabatId = new Input<int>(mokhtabatIdVar)
            }.WithLayout(x: 860, y: 100, w: 220);

            builder.Root = new Flowchart
            {
                Activities = { setAppId, createRecord, step1, step2 },
                Connections =
                {
                    new Connection(new Endpoint(setAppId,      "Done"), new Endpoint(createRecord)),
                    new Connection(new Endpoint(createRecord,  "Done"), new Endpoint(step1)),
                    new Connection(new Endpoint(step1,         "Done"), new Endpoint(step2))
                }
            };
        }
    }
}
