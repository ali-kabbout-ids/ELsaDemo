using Microsoft.EntityFrameworkCore;
using PurchaseOrderApi.Data;
using PurchaseOrderApi.Dtos;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Services;

public class ApplicationService(AppDbContext db)
{
    public async Task<ApplicationRequest> CreateAsync(StartApplicationRequest req)
    {
        ApplicationRequest app = new ApplicationRequest
        {
            TransactionType = req.TransactionType,
            EmployeeEmail = req.EmployeeEmail,
            I3almKanouniEmail = req.I3almKanouniEmail,
            Mo3awenCho3baEmail = req.Mo3awenCho3baEmail,
            RequiresMo5atabat = req.RequiresMo5atabat,
            CreatedAt = DateTime.UtcNow
        };
        db.ApplicationRequests.Add(app);
        await db.SaveChangesAsync();
        return app;
    }

    public async Task<ApplicationRequest?> GetByIdAsync(int id)
        => await db.ApplicationRequests.FindAsync(id);

    public async Task<IEnumerable<ApplicationRequest>> GetAllAsync()
        => await db.ApplicationRequests
                   .OrderByDescending(x => x.CreatedAt)
                   .ToListAsync();

    public async Task SaveAsync(ApplicationRequest app)
    {
        app.UpdatedAt = DateTime.UtcNow;
        db.ApplicationRequests.Update(app);
        await db.SaveChangesAsync();
    }
}