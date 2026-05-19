using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace PurchaseOrderApi.Studio.CustomWidgets
{
    public class TransactionTypeEditorWidget : IWidget
    {
        public string Zone => "workflow-definition-properties";

        public double Order => 15;

        public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
        {
            builder.OpenComponent<TransactionTypeWidget>(0);
            builder.AddAttribute(1, nameof(TransactionTypeWidget.WorkflowDefinition),
                attributes["WorkflowDefinition"]);
            builder.AddAttribute(2, nameof(TransactionTypeWidget.WorkflowDefinitionUpdated),
                attributes["WorkflowDefinitionUpdated"]);
            builder.CloseComponent();
        };
    }
}
