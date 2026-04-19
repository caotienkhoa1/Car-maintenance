using BE.models;

namespace BE.repository.IRepository
{
    public interface ICarOfAutoOwnerRepository
    {
        Task<List<Car>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<Car?> GetByIdAsync(long id);
        Task<List<Car>> GetByUserIdAsync(long userId);
        Task<List<Car>> GetServicedCarsByUserIdAsync(long userId);
        Task<Car> CreateAsync(Car car);
        Task<Car> UpdateAsync(Car car);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsLicensePlateAsync(string licensePlate, long? excludeCarId = null);
        Task<bool> ExistsVinNumberAsync(string vinNumber, long? excludeCarId = null);
        Task<bool> ExistsEngineNumberAsync(string engineNumber, long? excludeCarId = null);
    }
}
