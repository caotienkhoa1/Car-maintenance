using System;
using System.Linq;
using System.Threading.Tasks;
using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class TotalReceiptRepository : ITotalReceiptRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public TotalReceiptRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<TotalReceipt> AddAsync(TotalReceipt entity)
        {
            var entry = await _context.TotalReceipts.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.TotalReceipts.FindAsync(id);
            if (entity == null) return false;

            _context.TotalReceipts.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsByMaintenanceTicketIdAsync(long maintenanceTicketId, long? excludeId = null)
        {
            return await _context.TotalReceipts.AnyAsync(r => r.MaintenanceTicketId == maintenanceTicketId && (!excludeId.HasValue || r.Id != excludeId.Value));
        }

        public async Task<TotalReceipt?> GetByIdAsync(long id)
        {
            return await BuildQuery()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<TotalReceipt?> GetByMaintenanceTicketIdAsync(long maintenanceTicketId)
        {
            return await BuildQuery()
                .FirstOrDefaultAsync(r => r.MaintenanceTicketId == maintenanceTicketId);
        }

        public async Task<List<TotalReceipt>> GetListAsync(string? statusCode = null, long? branchId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = BuildQuery();

            if (!string.IsNullOrWhiteSpace(statusCode))
            {
                query = query.Where(r => r.StatusCode != null && r.StatusCode == statusCode);
            }

            if (branchId.HasValue)
            {
                query = query.Where(r => r.BranchId == branchId.Value || (r.MaintenanceTicket != null && r.MaintenanceTicket.BranchId == branchId.Value));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.CreatedAt <= to);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt ?? DateTime.UtcNow)
                .ThenByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task<TotalReceipt> UpdateAsync(TotalReceipt entity)
        {
            _context.TotalReceipts.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        private IQueryable<TotalReceipt> BuildQuery()
        {
            return _context.TotalReceipts
                .Include(r => r.MaintenanceTicket)
                    .ThenInclude(mt => mt.Car)
                        .ThenInclude(c => c.User)
                .Include(r => r.MaintenanceTicket)
                    .ThenInclude(mt => mt.Car)
                        .ThenInclude(c => c.VehicleType)
                .Include(r => r.MaintenanceTicket)
                    .ThenInclude(mt => mt.Branch)
                .Include(r => r.MaintenanceTicket)
                    .ThenInclude(mt => mt.ServiceTasks)
                .Include(r => r.MaintenanceTicket)
                    .ThenInclude(mt => mt.TicketComponents)
                .Include(r => r.MaintenanceTicket)
                    .ThenInclude(mt => mt.ServicePackage)
                        .ThenInclude(sp => sp.ComponentPackages)
                        .ThenInclude(cp => cp.Component) // Load components của package để tính giảm giá
                .Include(r => r.Car)
                .Include(r => r.Branch)
                .Include(r => r.Accountant)
                .Include(r => r.StatusCodeNavigation)
                .Include(r => r.ServicePackage)
                    .ThenInclude(sp => sp.ComponentPackages)
                        .ThenInclude(cp => cp.Component) // Load components của package trong receipt
                .AsQueryable();
        }
    }
}


