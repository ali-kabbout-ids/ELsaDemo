using PurchaseOrderApi.Enums;

namespace PurchaseOrderApi.Models
{
    public class MokhatabatRequest
    {
        public int Id { get; set; }

        public int ApplicationRequestId { get; set; }

        public ApplicationRequest ApplicationRequest { get; set; } = default!;

        public string? WorkflowInstanceId { get; set; } 

        public MokhatabatStatus Status { get; set; } = MokhatabatStatus.Started;

        public string? Step1Decision { get; set; }

        public string? Step1Reason { get; set; }

        public DateTime? Step1DecidedAt { get; set; }

        public string? Step2Decision { get; set; }

        public string? Step2Reason { get; set; }

        public DateTime? Step2DecidedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
