using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE.models;
using BE.repository.IRepository;

namespace BE.repository
{
    public class StockInRepository : IStockInRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public StockInRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockIn>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null)
        {
            var query = _context.StockIns
                .Include(si => si.StockInRequest)
                    .ThenInclude(sir => sir.Branch)
                .Include(si => si.StatusCodeNavigation)
                .Include(si => si.CreatedByNavigation)
                .Include(si => si.ApprovedByNavigation)
                .Include(si => si.StockInDetails)
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
            var query = _context.StockIns
                .Include(si => si.StockInRequest)
                .AsQueryable();
            query = ApplyFilters(query, branchId, statusCode, search);
            return await query.CountAsync();
        }

        public async Task<StockIn?> GetByIdAsync(long id)
        {
            return await _context.StockIns
                .Include(si => si.StockInRequest)
                    .ThenInclude(sir => sir.Branch)
                .Include(si => si.StatusCodeNavigation)
                .Include(si => si.CreatedByNavigation)
                .Include(si => si.ApprovedByNavigation)
                .Include(si => si.LastModifiedByNavigation)
                .Include(si => si.StockInDetails)
                    .ThenInclude(d => d.Component)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<StockIn?> GetByStockInRequestIdAsync(long stockInRequestId)
        {
            return await _context.StockIns
                .Include(si => si.StockInRequest)
                    .ThenInclude(sir => sir.Branch)
                .Include(si => si.StatusCodeNavigation)
                .Include(si => si.StockInDetails)
                    .ThenInclude(d => d.Component)
                .FirstOrDefaultAsync(x => x.StockInRequestId == stockInRequestId);
        }

        public async Task<IEnumerable<StockIn>> GetByStatusAsync(string statusCode)
        {
            return await _context.StockIns
                .Include(si => si.StockInRequest)
                    .ThenInclude(sir => sir.Branch)
                .Include(si => si.StatusCodeNavigation)
                .Include(si => si.StockInDetails)
                    .ThenInclude(d => d.Component)
                .Where(x => x.StatusCode == statusCode)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<StockIn> AddAsync(StockIn entity)
        {
            var added = await _context.StockIns.AddAsync(entity);
            await _context.SaveChangesAsync();
            return added.Entity;
        }

        public async Task<StockIn> UpdateAsync(StockIn entity)
        {
            _context.StockIns.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.StockIns.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsByStockInRequestIdAsync(long stockInRequestId)
        {
            return await _context.StockIns.AnyAsync(x => x.StockInRequestId == stockInRequestId);
        }

        private IQueryable<StockIn> ApplyFilters(IQueryable<StockIn> query, long? branchId, string? statusCode, string? search)
        {
            if (branchId.HasValue)
                query = query.Where(x => x.StockInRequest != null && x.StockInRequest.BranchId == branchId.Value);
            if (!string.IsNullOrEmpty(statusCode))
                query = query.Where(x => x.StatusCode == statusCode);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => 
                    (x.StockInRequest != null && x.StockInRequest.Code != null && x.StockInRequest.Code.ToLower().Contains(s)));
            }
            return query;
        }
    }
}

