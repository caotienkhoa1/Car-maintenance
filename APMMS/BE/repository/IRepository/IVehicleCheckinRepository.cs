using BE.models;

namespace BE.repository.IRepository
{
    public interface IVehicleCheckinRepository
    {
        Task<VehicleCheckin> CreateAsync(VehicleCheckin vehicleCheckin);
        Task<VehicleCheckin> UpdateAsync(VehicleCheckin vehicleCheckin);
        Task<VehicleCheckin?> GetByIdAsync(long id);
        Task<List<VehicleCheckin>> GetAllAsync(int page = 1, int pageSize = 10, string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<VehicleCheckin>> GetByCarIdAsync(long carId);
        Task<List<VehicleCheckin>> GetByMaintenanceRequestIdAsync(long maintenanceRequestId);
        Task<bool> DeleteAsync(long id);
        Task<VehicleCheckin?> GetByIdWithDetailsAsync(long id);
        Task<List<VehicleCheckin>> GetAllWithDetailsAsync(int page = 1, int pageSize = 10, string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? branchId = null);
        Task<int> GetTotalCountAsync(string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? branchId = null);
        Task<List<Car>> SearchCarsAsync(string searchTerm);
    }
}
