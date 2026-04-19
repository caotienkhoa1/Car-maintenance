using System.Collections.Generic;
using System.Threading.Tasks;
using BE.models;

namespace BE.repository.IRepository
{
    public interface IStockInRepository
    {
        Task<IEnumerable<StockIn>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null);
        Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null);
        Task<StockIn?> GetByIdAsync(long id);
        Task<StockIn?> GetByStockInRequestIdAsync(long stockInRequestId);
        Task<IEnumerable<StockIn>> GetByStatusAsync(string statusCode);
        Task<StockIn> AddAsync(StockIn entity);
        Task<StockIn> UpdateAsync(StockIn entity);
        Task<bool> ExistsAsync(long id);
        Task<bool> ExistsByStockInRequestIdAsync(long stockInRequestId);
    }
}

