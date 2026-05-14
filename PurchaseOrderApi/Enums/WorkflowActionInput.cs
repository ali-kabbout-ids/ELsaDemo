using Elsa.Workflows.Models;
using PurchaseOrderApi.Dtos;

namespace PurchaseOrderApi.Enums
{
    public static class WorkflowActionInput
    {
        // "approve,reject" — plain string, Elsa serializes perfectly
        public static Input<string> Keys(params WorkflowAction[] actions)
            => new(string.Join(",", actions.Select(a => a.Key)));
    }
}
