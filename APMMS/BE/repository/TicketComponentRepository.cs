using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE.models;
using BE.repository.IRepository;

namespace BE.repository
{
    public class TicketComponentRepository : ITicketComponentRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public TicketComponentRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<TicketComponent> AddAsync(TicketComponent entity)
        {
            var added = await _context.TicketComponents.AddAsync(entity);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task<TicketComponent?> GetByIdAsync(long id)
        {
            return await _context.TicketComponents
                .Include(tc => tc.Component)
                    .ThenInclude(c => c!.TypeComponent)
                .Include(tc => tc.MaintenanceTicket)
                .FirstOrDefaultAsync(tc => tc.Id == id);
        }

        public async Task<IEnumerable<TicketComponent>> GetByMaintenanceTicketIdAsync(long maintenanceTicketId)
        {
            return await _context.TicketComponents
                .Include(tc => tc.Component)
                    .ThenInclude(c => c!.TypeComponent)
                .Where(tc => tc.MaintenanceTicketId == maintenanceTicketId)
                .ToListAsync();
        }

        public async Task<TicketComponent?> UpdateAsync(TicketComponent entity)
        {
            var existing = await _context.TicketComponents.FindAsync(entity.Id);
            if (existing == null) return null;

            existing.ComponentId = entity.ComponentId;
            existing.BranchId = entity.BranchId; // ✅ Thêm lại BranchId
            existing.Quantity = entity.Quantity;
            existing.ActualQuantity = entity.ActualQuantity;
            existing.UnitPrice = entity.UnitPrice;

            _context.TicketComponents.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.TicketComponents.FindAsync(id);
            if (entity == null) return false;

            _context.TicketComponents.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.TicketComponents.AnyAsync(x => x.Id == id);
        }
    }
}

