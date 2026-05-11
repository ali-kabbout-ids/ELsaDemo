using Microsoft.EntityFrameworkCore;
using PurchaseOrderApi.Data;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Models;

namespace PurchaseOrderApi.Services
{
    public class MokhatabatService(AppDbContext db)
    {
        public async Task<MokhatabatRequest> CreateAsync(int applicationId)
        {
            MokhatabatRequest record = new MokhatabatRequest
            {
                ApplicationRequestId = applicationId,
                Status = MokhatabatStatus.Started,
                CreatedAt = DateTime.UtcNow
            };
            db.MokhatabatRequests.Add(record);
            await db.SaveChangesAsync();
            return record;
        }

        public async Task<MokhatabatRequest?> GetByIdAsync(int id)
            => await db.MokhatabatRequests.FindAsync(id);

        public async Task<MokhatabatRequest?> GetByApplicationIdAsync(int appId)
            => await db.MokhatabatRequests
                       .Where(x => x.ApplicationRequestId == appId)
        .OrderByDescending(x => x.CreatedAt)
                       .FirstOrDefaultAsync();

        public async Task SaveAsync(MokhatabatRequest record)
        {
            record.UpdatedAt = DateTime.UtcNow;
            db.MokhatabatRequests.Update(record);
            await db.SaveChangesAsync();
        }
    }
}
