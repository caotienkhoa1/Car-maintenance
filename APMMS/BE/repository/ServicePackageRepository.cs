using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class ServicePackageRepository : IServicePackageRepository
    {
        private readonly CarMaintenanceDbContext _context;
        public ServicePackageRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<ServicePackage> AddAsync(ServicePackage entity)
        {
            var added = await _context.ServicePackages.AddAsync(entity);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task DisableEnableAsync(long id, string statusCode)
        {
            var item = await _context.ServicePackages.FindAsync(id);
            if (item == null) return;
            item.StatusCode = statusCode;
            _context.ServicePackages.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.ServicePackages.AnyAsync(x => x.Id == id);
        }

        private IQueryable<ServicePackage> ApplyFilters(IQueryable<ServicePackage> query, long? branchId, string? statusCode, string? search)
        {
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId.Value);
            if (!string.IsNullOrEmpty(statusCode)) query = query.Where(x => x.StatusCode == statusCode);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => (x.Name != null && x.Name.ToLower().Contains(s)) || (x.Code != null && x.Code.ToLower().Contains(s)));
            }
            return query;
        }

        public async Task<IEnumerable<ServicePackage>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null)
        {
            var query = _context.ServicePackages
                .Include(sp => sp.ComponentPackages)
                    .ThenInclude(cp => cp.Component)
                .Include(sp => sp.Branch)
                .AsQueryable();

            query = ApplyFilters(query, branchId, statusCode, search);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null)
        {
            var query = _context.ServicePackages.AsQueryable();
            query = ApplyFilters(query, branchId, statusCode, search);
            return await query.CountAsync();
        }

        public async Task<ServicePackage?> GetByIdAsync(long id)
        {
            return await _context.ServicePackages
                .Include(sp => sp.ComponentPackages)
                    .ThenInclude(cp => cp.Component)
                .Include(sp => sp.ServicePackageCategories)
                    .ThenInclude(spc => spc.ServiceCategory)
                .Include(sp => sp.Branch)
                .FirstOrDefaultAsync(sp => sp.Id == id);
        }

        public async Task<ServicePackage> UpdateAsync(ServicePackage entity)
        {
            _context.ServicePackages.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}


