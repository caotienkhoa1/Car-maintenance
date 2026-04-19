using BE.models;

namespace BE.repository.IRepository
{
    public interface IServicePackageRepository
    {
        Task<IEnumerable<ServicePackage>> GetAllAsync(int page = 1, int pageSize = 10, long? branchId = null, string? statusCode = null, string? search = null);
        Task<int> GetTotalCountAsync(long? branchId = null, string? statusCode = null, string? search = null);
        Task<ServicePackage?> GetByIdAsync(long id);
        Task<ServicePackage> AddAsync(ServicePackage entity);
        Task<ServicePackage> UpdateAsync(ServicePackage entity);
        Task DisableEnableAsync(long id, string statusCode);
        Task<bool> ExistsAsync(long id);
    }
}


