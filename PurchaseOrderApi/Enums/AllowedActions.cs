namespace PurchaseOrderApi.Enums
{
    [Flags]
    public enum AllowedActions
    {
        None = 0,
        Approve = 1 << 0,
        Reject = 1 << 1,
        RequestInfo = 1 << 2,
        Acknowledge = 1 << 3
    }
}
