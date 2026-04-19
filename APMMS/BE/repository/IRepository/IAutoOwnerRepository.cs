using BE.models;

namespace BE.repository.IRepository
{
    public interface IAutoOwnerRepository
    {
        Task<List<User>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<List<User>> GetWithFiltersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, long? roleId = null);
        Task<int> GetTotalCountAsync(string? search = null, string? status = null, long? roleId = null);
        Task<User?> GetByIdAsync(long id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByPhoneAsync(string phone);
        Task<User?> GetByCitizenIdAsync(string citizenId);
        Task<List<User>> GetByCarIdAsync(long carId);
        Task<List<User>> GetByBranchIdAsync(long branchId);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(long id);
    }
}
