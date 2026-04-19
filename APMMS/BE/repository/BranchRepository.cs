using BE.repository.IRepository;
using BE.models;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class BranchRepository : IBranchRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public BranchRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Branch>> GetAllAsync()
        {
            return await _context.Branches.ToListAsync();
        }

        public async Task<Branch?> GetByIdAsync(long id)
        {
            return await _context.Branches.FindAsync(id);
        }

        public async Task AddAsync(Branch branch)
        {
            await _context.Branches.AddAsync(branch);
        }

        public async Task UpdateAsync(Branch branch)
        {
            _context.Branches.Update(branch);
        }

        public async Task DeleteAsync(Branch branch)
        {
            _context.Branches.Remove(branch);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsBranchInUseAsync(long id)
        {
            var hasUsers = await _context.Users.AnyAsync(u => u.BranchId == id);
            var hasCars = await _context.Cars.AnyAsync(c => c.BranchId == id);
            var hasComponents = await _context.Components.AnyAsync(c => c.BranchId == id);
            var hasMaintenanceTickets = await _context.MaintenanceTickets.AnyAsync(m => m.BranchId == id);
            var hasServicePackages = await _context.ServicePackages.AnyAsync(s => s.BranchId == id);
            var hasTypeComponents = await _context.TypeComponents.AnyAsync(t => t.BranchId == id);

            return hasUsers || hasCars || hasComponents || hasMaintenanceTickets || hasServicePackages || hasTypeComponents;
        }
    }
}

