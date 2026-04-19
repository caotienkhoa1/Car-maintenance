using System.Collections.Generic;
using System.Threading.Tasks;
using BE.models;

namespace BE.repository.IRepository
{
    public interface ITicketComponentRepository
    {
        Task<TicketComponent> AddAsync(TicketComponent entity);
        Task<TicketComponent?> GetByIdAsync(long id);
        Task<IEnumerable<TicketComponent>> GetByMaintenanceTicketIdAsync(long maintenanceTicketId);
        Task<TicketComponent?> UpdateAsync(TicketComponent entity);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
    }
}

