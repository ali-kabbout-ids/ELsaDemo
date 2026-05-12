// ⚠️  FIX: Removed "using Elsa.Workflows.Contracts" — that namespace does not exist in Elsa 3.x.
//          IWorkflowBuilder lives in Elsa.Workflows.

using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Activities;

namespace PurchaseOrderApi.Workflows;

public class PurchaseOrderApprovalWorkflow : WorkflowBase
{
    public static string DefinitionId => nameof(PurchaseOrderApprovalWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        // ── Typed Workflow Variables ──────────────────────────────────────────
        Elsa.Workflows.Memory.Variable<int> orderIdVar   = builder.WithVariable<int>("OrderId",    default);
        Elsa.Workflows.Memory.Variable<string> managerEmail = builder.WithVariable<string>("Manager",  "");
        Elsa.Workflows.Memory.Variable<string> decisionVar  = builder.WithVariable<string>("Decision", "");
        Elsa.Workflows.Memory.Variable<string> reasonVar    = builder.WithVariable<string>("Reason",   "");

        builder.Root = new Flowchart
        {
            Activities =
            {
                new ValidateOrderActivity
                {
                    OrderId      = new Input<int>(orderIdVar),
                    ManagerEmail = new Output<string>(managerEmail)
                },

                new NotifyManagerActivity
                {
                    OrderId      = new Input<int>(orderIdVar),
                    ManagerEmail = new Input<string>(managerEmail)
                },

                // Bookmark: workflow suspends here, resumes when decide API is called
                new WaitForApprovalActivity
                {
                    Decision = new Output<string>(decisionVar),
                    Reason   = new Output<string>(reasonVar)
                },

                // If/Else branching
                new If(ctx => decisionVar.Get(ctx) == "approved")
                {
                    Then = new ApproveOrderActivity
                    {
                        OrderId = new Input<int>(orderIdVar)
                    },
                    Else = new RejectOrderActivity
                    {
                        OrderId = new Input<int>(orderIdVar),
                        Reason  = new Input<string>(reasonVar)
                    }
                }
            }
        };
    }
}
