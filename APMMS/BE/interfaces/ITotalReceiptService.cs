using System;
using System.Threading.Tasks;
using BE.DTOs.TotalReceipt;

namespace BE.interfaces
{
    public interface ITotalReceiptService
    {
        Task<PagedResultDto<ResponseDto>> GetPagedAsync(int page, int pageSize, string? search = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? branchId = null, long? userId = null);
        Task<ResponseDto?> GetByIdAsync(long id);
        Task<ResponseDto?> GetByMaintenanceTicketIdAsync(long maintenanceTicketId);
        Task<ResponseDto> CreateAsync(RequestDto dto);
        Task<ResponseDto?> UpdateAsync(long id, RequestDto dto, long? currentUserId = null);
        Task<bool> DeleteAsync(long id);
    }
}


