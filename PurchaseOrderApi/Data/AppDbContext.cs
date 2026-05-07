using Microsoft.EntityFrameworkCore;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

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

        // Store enum as string so the DB is readable
        modelBuilder.Entity<PurchaseOrder>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
    }
}
