using BE.DTOs.MaintenanceTicket;

namespace BE.interfaces
{
    public interface IMaintenanceTicketService
    {
        Task<ResponseDto> CreateMaintenanceTicketAsync(RequestDto request);
        Task<ResponseDto> CreateFromVehicleCheckinAsync(CreateFromCheckinDto request);
        Task<ResponseDto> UpdateMaintenanceTicketAsync(long id, RequestDto request);
        Task<ResponseDto> GetMaintenanceTicketByIdAsync(long id);
        Task<List<ListResponseDto>> GetAllMaintenanceTicketsAsync(int page = 1, int pageSize = 10, long? branchId = null);
        Task<List<ListResponseDto>> GetMaintenanceTicketsByCarIdAsync(long carId);
        Task<List<ListResponseDto>> GetMaintenanceTicketsByStatusAsync(string statusCode);
        Task<bool> DeleteMaintenanceTicketAsync(long id);
        Task<ResponseDto> UpdateStatusAsync(long id, string statusCode);
        Task<ResponseDto> AssignTechnicianAsync(long id, long technicianId);
        Task<ResponseDto> AddTechniciansAsync(long id, List<long> technicianIds, long? primaryId);
        Task<ResponseDto> RemoveTechniciansAsync(long id);
        Task<ResponseDto> StartMaintenanceAsync(long id);
        Task<ResponseDto> CompleteMaintenanceAsync(long id, long? userId = null);
        Task<ResponseDto> CancelMaintenanceTicketAsync(long id);
        Task<List<BE.DTOs.HistoryLog.ResponseDto>> GetHistoryLogsAsync(long id);
        Task<ResponseDto> ApplyServicePackageAsync(long id, long servicePackageId);
        Task<ResponseDto> RemoveServicePackageAsync(long id);
        Task<List<UserMaintenanceHistoryDto>> GetMaintenanceHistoryByUserIdAsync(long userId);
    }
}


