using PurchaseOrderApi.Enums;

namespace PurchaseOrderApi.Models
{
    public class ApplicationRequest
    {
        public int Id { get; set; }

        public string TransactionType { get; set; } = string.Empty;  // from external "Fill Form"

        public string EmployeeEmail { get; set; } = string.Empty; 

        public string I3almKanouniEmail { get; set; } = string.Empty; 

        public string Mo3awenCho3baEmail { get; set; } = string.Empty;

        public bool RequiresMo5atabat { get; set; } = false;     

        // ── Status ────────────────────────────────────────────────────────────────
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

        public int ReviewRound { get; set; } = 1;            

        // ── Decisions recorded per step ───────────────────────────────────────────
        public string? I3almKanouniDecision { get; set; }   // "approved" | "rejected"

        public string? I3almKanouniReason { get; set; }

        public string? Mo3awenDecision { get; set; }   // "approved" | "rejected"

        public string? Mo3awenReason { get; set; }

        public string? Mo5atabatDecision { get; set; }   // "completed" | "rejected"

        public string? FinalMo3awenDecision { get; set; }   // final sign-off

        public string? HasMane3Decision { get; set; }   // "no_obstacle" | "has_obstacle"

        public string? HasMane3Reason { get; set; }

        public string? RejectionReason { get; set; }

        public string? CurrentRequiredRole { get; set; }

        public string? CurrentStepName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? WorkflowInstanceId { get; set; }
    }
}
