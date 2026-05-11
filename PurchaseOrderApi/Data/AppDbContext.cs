using Microsoft.EntityFrameworkCore;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<ApplicationRequest> ApplicationRequests => Set<ApplicationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>(b =>
        {
            b.ToTable("PurchaseOrders");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title)              .HasMaxLength(500) .IsRequired();
            b.Property(x => x.Amount)             .HasColumnType("decimal(18,2)");
            b.Property(x => x.RequesterEmail)     .HasMaxLength(256) .IsRequired();
            b.Property(x => x.ManagerEmail)       .HasMaxLength(256) .IsRequired();
            b.Property(x => x.RejectionReason)    .HasMaxLength(2000);
            b.Property(x => x.WorkflowInstanceId) .HasMaxLength(256);
        });

        modelBuilder.Entity<ApplicationRequest>(b =>
        {
            b.ToTable("ApplicationRequests");
            b.HasKey(x => x.Id);

            b.Property(x => x.TransactionType).HasMaxLength(100).IsRequired();
            b.Property(x => x.EmployeeEmail).HasMaxLength(256).IsRequired();
            b.Property(x => x.I3almKanouniEmail).HasMaxLength(256).IsRequired();
            b.Property(x => x.Mo3awenCho3baEmail).HasMaxLength(256).IsRequired();

            b.Property(x => x.I3almKanouniDecision).HasMaxLength(300);
            b.Property(x => x.I3almKanouniReason).HasMaxLength(2000);

            b.Property(x => x.Mo3awenDecision).HasMaxLength(300);
            b.Property(x => x.Mo3awenReason).HasMaxLength(2000);

            b.Property(x => x.HasMane3Decision).HasMaxLength(50);
            b.Property(x => x.HasMane3Reason).HasMaxLength(2000);

            b.Property(x => x.RejectionReason).HasMaxLength(2000);

            // Elsa Workflow Mapping
            b.Property(x => x.WorkflowInstanceId).HasMaxLength(255);
            b.HasIndex(x => x.WorkflowInstanceId);

            // Status Enum Conversion
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
        });

        // Store enum as string so the DB is readable
        modelBuilder.Entity<PurchaseOrder>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
    }
}
