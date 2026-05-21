using Elsa.Models;
using Elsa.Workflows.Api;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Api.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace PurchaseOrderApi.Providers
{
    public class PermissionAwareWorkflowDefinitionLinker(
    StaticWorkflowDefinitionLinker inner,
    IHttpContextAccessor httpContextAccessor)
    : IWorkflowDefinitionLinker
    {
        private static readonly Dictionary<string, string> LinkPermissions = new()
        {
            ["bulk-publish"] = "publish:workflow-definitions",
            ["bulk-retract"] = "retract:workflow-definitions",
            ["bulk-delete-by-definition-id"] = "delete:workflow-definitions",
            ["bulk-delete-by-id"] = "delete:workflow-definitions",
            ["import"] = "write:workflow-definitions",
            ["import-files"] = "write:workflow-definitions",
            ["create"] = "write:workflow-definitions",
            ["publish"] = "publish:workflow-definitions",
            ["retract"] = "retract:workflow-definitions",
            ["delete"] = "delete:workflow-definitions",
            ["update-references"] = "write:workflow-definitions",
        };

        private bool HasPermission(string permission)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user == null) return false;
            return user.HasClaim("permissions", "*") ||
                   user.HasClaim("permissions", permission);
        }

        private Link[] FilterLinks(Link[]? links)
        {
            if (links == null) return [];
            return links
                .Where(l => !LinkPermissions.TryGetValue(l.Rel, out var required) || HasPermission(required))
                .ToArray();
        }

        /// <inheritdoc />  
        public async Task<LinkedWorkflowDefinitionModel> MapAsync(
            WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var model = await inner.MapAsync(definition, cancellationToken);

            return new LinkedWorkflowDefinitionModel(FilterLinks(model.Links))
            {
                Id = model.Id,
                DefinitionId = model.DefinitionId,
                Name = model.Name,
                Description = model.Description,
                CreatedAt = model.CreatedAt,
                Version = model.Version,
                ToolVersion = model.ToolVersion,
                Variables = model.Variables,
                Inputs = model.Inputs,
                Outputs = model.Outputs,
                Outcomes = model.Outcomes,
                CustomProperties = model.CustomProperties,
                IsReadonly = model.IsReadonly,
                IsSystem = model.IsSystem,
                IsLatest = model.IsLatest,
                IsPublished = model.IsPublished,
                Options = model.Options,
                UsableAsActivity = model.UsableAsActivity,
                Root = model.Root
            };
        }

        /// <inheritdoc />  
        public PagedListResponse<LinkedWorkflowDefinitionSummary> MapAsync(
            PagedListResponse<WorkflowDefinitionSummary> list, CancellationToken cancellationToken = default)
        {
            var response = inner.MapAsync(list, cancellationToken);

            foreach (var item in response.Items)
                item.Links = FilterLinks(item.Links);

            return response with { Links = FilterLinks(response.Links) };
        }

        /// <inheritdoc />  
        public async Task<List<LinkedWorkflowDefinitionModel>> MapAsync(
            List<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
        {
            var models = await inner.MapAsync(definitions, cancellationToken);
            var result = new List<LinkedWorkflowDefinitionModel>(models.Count);

            foreach (var model in models)
            {
                result.Add(new LinkedWorkflowDefinitionModel(FilterLinks(model.Links))
                {
                    Id = model.Id,
                    DefinitionId = model.DefinitionId,
                    Name = model.Name,
                    Description = model.Description,
                    CreatedAt = model.CreatedAt,
                    Version = model.Version,
                    ToolVersion = model.ToolVersion,
                    Variables = model.Variables,
                    Inputs = model.Inputs,
                    Outputs = model.Outputs,
                    Outcomes = model.Outcomes,
                    CustomProperties = model.CustomProperties,
                    IsReadonly = model.IsReadonly,
                    IsSystem = model.IsSystem,
                    IsLatest = model.IsLatest,
                    IsPublished = model.IsPublished,
                    Options = model.Options,
                    UsableAsActivity = model.UsableAsActivity,
                    Root = model.Root
                });
            }

            return result;
        }
    }
}
