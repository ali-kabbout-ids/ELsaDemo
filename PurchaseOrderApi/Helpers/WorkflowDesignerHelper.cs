using Elsa.Workflows;

namespace PurchaseOrderApi.Helpers
{
    public static class WorkflowDesignerHelper
    {
        public static T WithLayout<T>(this T activity, double x, double y, double w = 200, double h = 50, string? displayText = null)
                where T : Activity
        {
            activity.Metadata ??= new Dictionary<string, object>();

            // Set the visual label in metadata where Elsa looks for it
            if (!string.IsNullOrEmpty(displayText))
            {
                activity.Metadata["displayText"] = displayText;
            }

            activity.Metadata["designer"] = new Dictionary<string, object>
            {
                ["position"] = new Dictionary<string, object> { ["x"] = x, ["y"] = y },
                ["size"] = new Dictionary<string, object> { ["width"] = w, ["height"] = h }
            };

            return activity;
        }
    }
}
