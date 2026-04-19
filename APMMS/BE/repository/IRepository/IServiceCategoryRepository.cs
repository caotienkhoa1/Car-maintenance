using BE.models;

namespace BE.repository.IRepository
{
    public interface IServiceCategoryRepository
    {
        Task<ServiceCategory?> GetByIdAsync(long id);
        Task<List<ServiceCategory>> GetAllAsync();
    }
}

