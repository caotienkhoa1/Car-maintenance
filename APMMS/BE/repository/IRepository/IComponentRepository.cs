using System.Collections.Generic;
using System.Threading.Tasks;
using BE.models;

namespace BE.repository.IRepository
{
    public interface IComponentRepository
    {
        Task<IEnumerable<Component>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, long? typeComponentId = null, string? statusCode = null, string? search = null);
        Task<int> GetTotalCountAsync(long? branchId = null, long? typeComponentId = null, string? statusCode = null, string? search = null);
        Task<Component?> GetByIdAsync(long id);
        Task<Component?> GetByCodeAsync(string code);
        Task<Component> AddAsync(Component entity);
        Task<Component> UpdateAsync(Component entity);
        Task DisableEnableAsync(long id, string statusCode);
        Task<bool> ExistsAsync(long id);
    }
}