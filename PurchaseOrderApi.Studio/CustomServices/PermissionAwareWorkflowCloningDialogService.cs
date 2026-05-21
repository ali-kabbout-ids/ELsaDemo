using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace PurchaseOrderApi.Studio.CustomServices
{
    public class PermissionAwareWorkflowCloningDialogService : IWorkflowCloningDialogService
    {
        private readonly IWorkflowCloningDialogService _inner;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IUserMessageService _userMessageService;

        public PermissionAwareWorkflowCloningDialogService(
            IWorkflowCloningDialogService inner,
            AuthenticationStateProvider authStateProvider,
            IUserMessageService userMessageService)
        {
            _inner = inner;
            _authStateProvider = authStateProvider;
            _userMessageService = userMessageService;
        }

        public async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>?> Duplicate(WorkflowDefinition workflowDefinition)
        {
            if (!await CanWriteAsync())
            {
                _userMessageService.ShowSnackbarTextMessage(
                    "You do not have permission to duplicate workflows.",
                    Severity.Warning);
                return null;
            }

            return await _inner.Duplicate(workflowDefinition);
        }

        public async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>?> SaveAs(WorkflowDefinition workflowDefinition)
        {
            if (!await CanWriteAsync())
            {
                _userMessageService.ShowSnackbarTextMessage(
                    "You do not have permission to save workflows.",
                    Severity.Warning);
                return null;
            }

            return await _inner.SaveAs(workflowDefinition);
        }

        private async Task<bool> CanWriteAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var permissions = authState.User.FindAll("permissions")
                                            .Select(c => c.Value)
                                            .ToHashSet();

            // Adjust to match what your admin token's "permissions" claim contains  
            return permissions.Contains("write:workflow-definitions")
                || permissions.Contains("publish:workflow-definitions");
        }
    }
}
