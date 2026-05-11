namespace PurchaseOrderApi.Enums
{
    public enum MokhatabatStatus
    {
        Started,          // Mokhatabat workflow just started
        PendingStep1,     // waiting for step 1 officer
        PendingStep2,     // waiting for step 2 officer
        Completed,        // both steps done
        Rejected          // rejected at some step
    }
}
