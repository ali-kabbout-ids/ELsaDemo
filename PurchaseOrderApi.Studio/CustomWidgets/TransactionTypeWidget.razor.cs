using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;

namespace PurchaseOrderApi.Studio.CustomWidgets
{
    public static class TransactionTypes
    {
        public const string ApplicationRequest = "application_request";

        public const string AdministrativeDecision = "administrative_decision";
        public const string InternalMemo = "internal_memo";                   

        public const string LegalNotification = "legal_notification";           // الإعلام القانوني والإشعارات الرسمية
        public const string ComplianceReview = "compliance_review";             // التدقيق القانوني ومراجعة المعاملات

        public const string CitizenGrievance = "citizen_grievance";             // الشكاوى والتظلمات المقدمة من المواطنين

        public static readonly string[] All =
        [
            ApplicationRequest,
            AdministrativeDecision,
            InternalMemo,
            LegalNotification,
            ComplianceReview,
            CitizenGrievance
        ];
    }

    public partial class TransactionTypeWidget : ComponentBase
    {
        [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;

        [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }

        [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = default!;

        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private string? _selectedType;
        private bool _saving;

        protected override void OnParametersSet()
        {
            _selectedType = WorkflowDefinition.CustomProperties
                .TryGetValue("transactionType", out var v)
                    ? (v is JsonElement je ? je.GetString() : v?.ToString())
                    : null;
        }

        private async Task OnTypeChangedAsync(string? newType)
        {
            _selectedType = newType;
            _saving = true;

            try
            {
                var api = await BackendApiClientProvider
                    .GetApiAsync<IWorkflowDefinitionsApi>();

                var full = await api.GetByIdAsync(WorkflowDefinition.Id);

                if (full is null)
                {
                    Snackbar.Add("Workflow definition not found.", Severity.Error);
                    return;
                }

                var props = full.CustomProperties ?? new Dictionary<string, object>();

                props["transactionType"] = newType ?? string.Empty;

                var request = new SaveWorkflowDefinitionRequest
                {
                    Model = new WorkflowDefinitionModel
                    {
                        Id = full.Id,
                        DefinitionId = full.DefinitionId,
                        Name = full.Name,
                        Description = full.Description,
                        ToolVersion = full.ToolVersion,
                        Inputs = full.Inputs,
                        Outputs = full.Outputs,
                        Outcomes = full.Outcomes,
                        Options = full.Options,
                        CustomProperties = props,
                        Root = full.Root,
                        Variables = null
                    }
                };

                var response = await api.SaveAsync(request);
                var updatedDefinitionModel = response.WorkflowDefinition;
 
                WorkflowDefinition.CustomProperties = updatedDefinitionModel.CustomProperties;
                WorkflowDefinition.Id = updatedDefinitionModel.Id;
  
                await WorkflowDefinitionUpdated.InvokeAsync(WorkflowDefinition);

                _selectedType = newType;

                string debugProps = string.Join(" | ", response.WorkflowDefinition.CustomProperties.Select(p => $"{p.Key}: {p.Value}"));

                Snackbar.Add($"Saved! ", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to save: {ex.Message}", Severity.Error);
            }
            finally
            {
                _saving = false;
                StateHasChanged();
            }
        }
    }
}
