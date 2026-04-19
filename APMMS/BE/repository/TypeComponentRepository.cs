using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE.models;
using BE.repository.IRepository;
namespace BE.repository
{
    public class TypeComponentRepository : ITypeComponentRepository
    {
        private readonly CarMaintenanceDbContext _context;
        public TypeComponentRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<TypeComponent> AddAsync(TypeComponent entity)
        {
            var added = await _context.TypeComponents.AddAsync(entity);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task DisableEnableAsync(long id, string statusCode)
        {
            var item = await _context.TypeComponents.FindAsync(id);
            if (item == null) return;
            item.StatusCode = statusCode;
            _context.TypeComponents.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.TypeComponents.AnyAsync(x => x.Id == id);
        }

        public async Task<List<TypeComponent>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null)
        {
            var q = _context.TypeComponents
                .Include(tc => tc.Branch)
                .AsQueryable();
            
            if (branchId.HasValue) q = q.Where(x => x.BranchId == branchId.Value);
            if (!string.IsNullOrEmpty(statusCode)) q = q.Where(x => x.StatusCode == statusCode);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => (x.Name != null && x.Name.ToLower().Contains(s)) || 
                                 (x.Description != null && x.Description.ToLower().Contains(s)));
            }
            
            return await q
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null)
        {
            var q = _context.TypeComponents.AsQueryable();
            
            if (branchId.HasValue) q = q.Where(x => x.BranchId == branchId.Value);
            if (!string.IsNullOrEmpty(statusCode)) q = q.Where(x => x.StatusCode == statusCode);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => (x.Name != null && x.Name.ToLower().Contains(s)) || 
                                 (x.Description != null && x.Description.ToLower().Contains(s)));
            }
            
            return await q.CountAsync();
        }

        public async Task<TypeComponent?> GetByIdAsync(long id)
        {
            return await _context.TypeComponents
                .Include(tc => tc.Branch)
                .FirstOrDefaultAsync(tc => tc.Id == id);
        }

        public async Task<TypeComponent> UpdateAsync(TypeComponent entity)
        {
            _context.TypeComponents.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}