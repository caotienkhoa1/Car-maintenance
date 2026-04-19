using BE.models;

namespace BE.repository.IRepository
{
    public interface IBranchRepository
    {
        Task<IEnumerable<Branch>> GetAllAsync();
        Task<Branch?> GetByIdAsync(long id);
        Task AddAsync(Branch branch);
        Task UpdateAsync(Branch branch);
        Task DeleteAsync(Branch branch);
        Task SaveChangesAsync();
        Task<bool> IsBranchInUseAsync(long id);
    }
}

