using BE.DTOs.VehicleCheckin;

namespace BE.interfaces
{
    public interface IVehicleCheckinService
    {
        Task<ResponseDto> CreateVehicleCheckinAsync(VehicleCheckinRequestDto request, long? createdByUserId = null);
        Task<ResponseDto> UpdateVehicleCheckinAsync(UpdateDto request);
        Task<ResponseDto> GetVehicleCheckinByIdAsync(long id);
        Task<List<ListResponseDto>> GetAllVehicleCheckinsAsync(int page = 1, int pageSize = 10, string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? userId = null, long? branchId = null);
        Task<int> GetTotalCountAsync(string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? userId = null, long? branchId = null);
        Task<List<ListResponseDto>> GetVehicleCheckinsByCarIdAsync(long carId);
        Task<List<ListResponseDto>> GetVehicleCheckinsByMaintenanceRequestIdAsync(long maintenanceRequestId);
        Task<ResponseDto> LinkMaintenanceRequestAsync(long vehicleCheckinId, long maintenanceRequestId);
        Task<bool> DeleteVehicleCheckinAsync(long id);
        Task<object> SearchVehicleAsync(string searchTerm);
        Task<ResponseDto> UpdateStatusAsync(long id, string statusCode);
    }
}
