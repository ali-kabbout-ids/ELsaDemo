namespace PurchaseOrderApi.Dtos
{
    public record StartApplicationRequest(
        string TransactionType,
        string EmployeeEmail,
        string I3almKanouniEmail,
        string Mo3awenCho3baEmail,
        bool RequiresMo5atabat = false);
}
