using System.Collections.Generic;
using System.Threading.Tasks;
using BE.models;

namespace BE.repository.IRepository
{
    public interface ITypeComponentRepository
    {
        Task<List<TypeComponent>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null);
        Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null);
        Task<TypeComponent?> GetByIdAsync(long id);
        Task<TypeComponent> AddAsync(TypeComponent entity);
        Task<TypeComponent> UpdateAsync(TypeComponent entity);
        Task DisableEnableAsync(long id, string statusCode);
        Task<bool> ExistsAsync(long id);
    }
}