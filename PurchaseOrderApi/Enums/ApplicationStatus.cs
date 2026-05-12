namespace PurchaseOrderApi.Enums
{
    public enum ApplicationStatus
    {
        Submitted,
        PendingI3almKanouniReview,   // waiting for Legal Advisor
        PendingMo3awenReview,        // waiting for Dept Assistant
        PendingMo5atabat,            // sub-workflow running
        PendingFinalMo3awenReview,   // waiting for final Mo3awen sign-off
        PendingHasMane3Check,        // waiting for legal obstacle check
        Approved,
        Rejected
    }
}
