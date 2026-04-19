using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE.models;
using BE.repository.IRepository;

namespace BE.repository
{
    public class StockInRequestRepository : IStockInRequestRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public StockInRequestRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockInRequest>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null)
        {
            var query = _context.StockInRequests
                .Include(sir => sir.Branch)
                .Include(sir => sir.StatusCodeNavigation)
                .Include(sir => sir.CreatedByNavigation)
                .Include(sir => sir.StockInRequestDetails)
                    .ThenInclude(d => d.Component)
                .AsQueryable();

            query = ApplyFilters(query, branchId, statusCode, search);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null)
        {
            var query = _context.StockInRequests.AsQueryable();
            query = ApplyFilters(query, branchId, statusCode, search);
            return await query.CountAsync();
        }

        public async Task<StockInRequest?> GetByIdAsync(long id)
        {
            return await _context.StockInRequests
                .Include(sir => sir.Branch)
                .Include(sir => sir.StatusCodeNavigation)
                .Include(sir => sir.CreatedByNavigation)
                .Include(sir => sir.LastModifiedByNavigation)
                .Include(sir => sir.StockInRequestDetails)
                    .ThenInclude(d => d.Component)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<StockInRequest?> GetByCodeAsync(string code)
        {
            return await _context.StockInRequests
                .Include(sir => sir.Branch)
                .Include(sir => sir.StatusCodeNavigation)
                .Include(sir => sir.StockInRequestDetails)
                    .ThenInclude(d => d.Component)
                .FirstOrDefaultAsync(x => x.Code == code);
        }

        public async Task<StockInRequest?> GetByCodeAndStatusAsync(string code, string statusCode)
        {
            return await _context.StockInRequests
                .Include(sir => sir.Branch)
                .Include(sir => sir.StatusCodeNavigation)
                .Include(sir => sir.StockInRequestDetails)
                    .ThenInclude(d => d.Component)
                .FirstOrDefaultAsync(x => x.Code == code && x.StatusCode == statusCode);
        }

        public async Task<IEnumerable<StockInRequest>> GetByStatusAsync(string statusCode)
        {
            return await _context.StockInRequests
                .Include(sir => sir.Branch)
                .Include(sir => sir.StatusCodeNavigation)
                .Include(sir => sir.StockInRequestDetails)
                    .ThenInclude(d => d.Component)
                .Where(x => x.StatusCode == statusCode)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<StockInRequest> AddAsync(StockInRequest entity)
        {
            var added = await _context.StockInRequests.AddAsync(entity);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task<StockInRequest> UpdateAsync(StockInRequest entity)
        {
            _context.StockInRequests.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.StockInRequests.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
        {
            var query = _context.StockInRequests.Where(x => x.Code == code);
            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        private IQueryable<StockInRequest> ApplyFilters(IQueryable<StockInRequest> query, long? branchId, string? statusCode, string? search)
        {
            if (branchId.HasValue)
                query = query.Where(x => x.BranchId == branchId.Value);
            if (!string.IsNullOrEmpty(statusCode))
                query = query.Where(x => x.StatusCode == statusCode);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => 
                    (x.Code != null && x.Code.ToLower().Contains(s)) ||
                    (x.Description != null && x.Description.ToLower().Contains(s)) ||
                    (x.Note != null && x.Note.ToLower().Contains(s)));
            }
            return query;
        }
    }
}

