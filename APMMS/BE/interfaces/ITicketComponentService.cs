using System.Collections.Generic;
using System.Threading.Tasks;
using BE.DTOs.TicketComponent;

namespace BE.interfaces
{
    public interface ITicketComponentService
    {
        Task<ResponseDto> CreateAsync(RequestDto dto, long? userId = null);
        Task<ResponseDto?> GetByIdAsync(long id);
        Task<IEnumerable<ResponseDto>> GetByMaintenanceTicketIdAsync(long maintenanceTicketId);
        Task<ResponseDto?> UpdateAsync(long id, RequestDto dto, long? userId = null);
        Task<bool> DeleteAsync(long id);
        Task<decimal> CalculateTotalCostAsync(long maintenanceTicketId);
    }
}

