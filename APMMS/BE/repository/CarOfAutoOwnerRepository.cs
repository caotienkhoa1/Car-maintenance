using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class CarOfAutoOwnerRepository : ICarOfAutoOwnerRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public CarOfAutoOwnerRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<List<Car>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Cars
                .Include(c => c.VehicleType)
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Car?> GetByIdAsync(long id)
        {
            return await _context.Cars
                .Include(c => c.VehicleType)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Car>> GetByUserIdAsync(long userId)
        {
            return await _context.Cars
                .Include(c => c.VehicleType)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Car>> GetServicedCarsByUserIdAsync(long userId)
        {
            // ✅ Chỉ lấy những xe đã từng được bảo dưỡng (có ScheduleService hoặc MaintenanceTicket)
            // Lấy danh sách carId từ ScheduleServices của user này
            var carIdsWithSchedule = await _context.ScheduleServices
                .Where(ss => ss.CarId != null)
                .Join(_context.Cars.Where(c => c.UserId == userId),
                    ss => ss.CarId,
                    c => c.Id,
                    (ss, c) => c.Id)
                .Distinct()
                .ToListAsync();

            // Lấy danh sách carId từ MaintenanceTickets của user này
            var carIdsWithTicket = await _context.MaintenanceTickets
                .Where(mt => mt.CarId != null)
                .Join(_context.Cars.Where(c => c.UserId == userId),
                    mt => mt.CarId,
                    c => c.Id,
                    (mt, c) => c.Id)
                .Distinct()
                .ToListAsync();

            // Hợp nhất 2 danh sách
            var servicedCarIds = carIdsWithSchedule.Union(carIdsWithTicket).Distinct().ToList();

            if (!servicedCarIds.Any())
            {
                return new List<Car>();
            }

            // Lấy thông tin đầy đủ các xe đã bảo dưỡng
            return await _context.Cars
                .Include(c => c.VehicleType)
                .Where(c => servicedCarIds.Contains(c.Id))
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<Car> CreateAsync(Car car)
        {
            try
            {
                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                return car;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Re-throw to be handled by service/controller
                throw;
            }
        }

        public async Task<Car> UpdateAsync(Car car)
        {
            _context.Cars.Update(car);
            await _context.SaveChangesAsync();
            return car;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.Cars.FindAsync(id);
            if (existing == null)
                return false;

            _context.Cars.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsLicensePlateAsync(string licensePlate, long? excludeCarId = null)
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
                return false;

            var normalized = licensePlate.Trim().ToUpper();
            var query = _context.Cars.Where(c => c.LicensePlate != null && c.LicensePlate.ToUpper() == normalized);
            if (excludeCarId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCarId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> ExistsVinNumberAsync(string vinNumber, long? excludeCarId = null)
        {
            if (string.IsNullOrWhiteSpace(vinNumber))
                return false;

            var normalized = vinNumber.Trim().ToUpper();
            var query = _context.Cars.Where(c => c.VinNumber != null && c.VinNumber.ToUpper() == normalized);
            if (excludeCarId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCarId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> ExistsEngineNumberAsync(string engineNumber, long? excludeCarId = null)
        {
            if (string.IsNullOrWhiteSpace(engineNumber))
                return false;

            var normalized = engineNumber.Trim().ToUpper();
            var query = _context.Cars.Where(c => c.VehicleEngineNumber != null && c.VehicleEngineNumber.ToUpper() == normalized);
            if (excludeCarId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCarId.Value);
            }
            return await query.AnyAsync();
        }
    }
}
