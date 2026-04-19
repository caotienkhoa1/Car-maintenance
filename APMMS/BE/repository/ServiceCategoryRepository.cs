using BE.repository.IRepository;
using BE.models;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class ServiceCategoryRepository : IServiceCategoryRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public ServiceCategoryRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceCategory?> GetByIdAsync(long id)
        {
            return await _context.ServiceCategories
                .FirstOrDefaultAsync(sc => sc.Id == id);
        }

        public async Task<List<ServiceCategory>> GetAllAsync()
        {
            return await _context.ServiceCategories
                .ToListAsync();
        }
    }
}

