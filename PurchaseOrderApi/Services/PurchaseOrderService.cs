using Microsoft.EntityFrameworkCore;
using PurchaseOrderApi.Data;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Services;

public class PurchaseOrderService(AppDbContext db)
{
    public async Task<PurchaseOrder> CreateAsync(CreateOrderRequest req)
    {
        PurchaseOrder order = new PurchaseOrder
        {
            Title = req.Title,
            Amount = req.Amount,
            RequesterEmail = req.RequesterEmail,
            ManagerEmail = req.ManagerEmail,
            CreatedAt = DateTime.UtcNow
        };
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
        => await db.PurchaseOrders.FindAsync(id);

    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        => await db.PurchaseOrders
                   .OrderByDescending(x => x.CreatedAt)
                   .ToListAsync();

    public async Task SaveAsync(PurchaseOrder order)
    {
        db.PurchaseOrders.Update(order);
        await db.SaveChangesAsync();
    }
}
