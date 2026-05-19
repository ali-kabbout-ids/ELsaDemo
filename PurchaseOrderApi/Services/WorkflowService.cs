using Elsa.Common.Models;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;

namespace PurchaseOrderApi.Services
{
    public class WorkflowService
    {
        private readonly IWorkflowDefinitionStore _defStore;

        public WorkflowService(
            IWorkflowDefinitionStore defStore)
        {
            _defStore = defStore;
        }

        public async Task<IEnumerable<WorkflowDefinition>> FindAllDefinitionsByType(
            string transactionType, CancellationToken ct = default)
        {
            WorkflowDefinitionFilter filter = new WorkflowDefinitionFilter { VersionOptions = VersionOptions.Published };
            var allPublished = await _defStore.FindManyAsync(filter, ct);

            return allPublished.Where(d =>
                d.CustomProperties.TryGetValue("transactionType", out var val) &&
                val?.ToString() == transactionType);
        }
    }
}
