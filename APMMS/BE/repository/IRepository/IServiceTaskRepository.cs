using BE.models;

namespace BE.repository.IRepository
{
    public interface IServiceTaskRepository
    {
        Task<ServiceTask> CreateAsync(ServiceTask serviceTask);
        Task<ServiceTask?> GetByIdAsync(long id);
        Task<ServiceTask?> GetByIdWithDetailsAsync(long id);
        Task<List<ServiceTask>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<List<ServiceTask>> GetAllWithDetailsAsync(int page = 1, int pageSize = 10);
        Task<List<ServiceTask>> GetByMaintenanceTicketIdAsync(long maintenanceTicketId);
        Task<List<ServiceTask>> GetByStatusAsync(string statusCode);
        Task<List<ServiceTask>> GetByTechnicianIdAsync(long technicianId);
        Task<ServiceTask> UpdateAsync(ServiceTask serviceTask);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
    }
}