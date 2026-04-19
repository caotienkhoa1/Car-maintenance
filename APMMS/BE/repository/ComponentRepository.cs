using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE.models;
using BE.repository.IRepository;

namespace BE.repository
{
    public class ComponentRepository : IComponentRepository
    {
        private readonly CarMaintenanceDbContext _context;
        public ComponentRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<Component> AddAsync(Component entity)
        {
            var added = await _context.Components.AddAsync(entity);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task DisableEnableAsync(long id, string statusCode)
        {
            var item = await _context.Components.FindAsync(id);
            if (item == null) return;
            item.StatusCode = statusCode;
            _context.Components.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.Components.AnyAsync(x => x.Id == id);
        }

        private IQueryable<Component> ApplyFilters(IQueryable<Component> query, long? branchId, long? typeComponentId, string? statusCode, string? search)
        {
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId.Value);
            if (typeComponentId.HasValue) query = query.Where(x => x.TypeComponentId == typeComponentId.Value);
            if (!string.IsNullOrEmpty(statusCode)) query = query.Where(x => x.StatusCode == statusCode);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => (x.Name != null && x.Name.ToLower().Contains(s)) || (x.Code != null && x.Code.ToLower().Contains(s)));
            }
            return query;
        }

        public async Task<IEnumerable<Component>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, long? typeComponentId = null, string? statusCode = null, string? search = null)
        {
            var query = _context.Components
                .Include(c => c.TypeComponent)
                .Include(c => c.Branch)
                .AsQueryable();

            query = ApplyFilters(query, branchId, typeComponentId, statusCode, search);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(long? branchId = null, long? typeComponentId = null, string? statusCode = null, string? search = null)
        {
            var query = _context.Components.AsQueryable();
            query = ApplyFilters(query, branchId, typeComponentId, statusCode, search);
            return await query.CountAsync();
        }

        public async Task<Component?> GetByIdAsync(long id)
        {
            return await _context.Components
                .Include(c => c.TypeComponent)
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Component?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            
            return await _context.Components
                .Include(c => c.TypeComponent)
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.Code != null && c.Code.ToLower() == code.ToLower());
        }

        public async Task<Component> UpdateAsync(Component entity)
        {
            _context.Components.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}