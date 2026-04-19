using BE.repository.IRepository;
using BE.models;
using Microsoft.EntityFrameworkCore;

namespace BE.repository
{
    public class VehicleCheckinRepository : IVehicleCheckinRepository
    {
        private readonly CarMaintenanceDbContext _context;

        public VehicleCheckinRepository(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        public async Task<VehicleCheckin> CreateAsync(VehicleCheckin vehicleCheckin)
        {
            _context.VehicleCheckins.Add(vehicleCheckin);
            await _context.SaveChangesAsync();
            return vehicleCheckin;
        }

        public async Task<VehicleCheckin> UpdateAsync(VehicleCheckin vehicleCheckin)
        {
            _context.VehicleCheckins.Update(vehicleCheckin);
            await _context.SaveChangesAsync();
            return vehicleCheckin;
        }

        public async Task<VehicleCheckin?> GetByIdAsync(long id)
        {
            return await _context.VehicleCheckins
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.Branch)
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.User)
                .Include(vc => vc.Branch)
                .Include(vc => vc.VehicleCheckinImages)
                .FirstOrDefaultAsync(vc => vc.Id == id);
        }

        public async Task<List<VehicleCheckin>> GetAllAsync(int page = 1, int pageSize = 10, string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.VehicleCheckins
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.Branch)
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.User)
                .Include(vc => vc.Branch)
                .Include(vc => vc.VehicleCheckinImages)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(vc => 
                    (vc.Car != null && vc.Car.LicensePlate != null && vc.Car.LicensePlate.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.VinNumber != null && vc.Car.VinNumber.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.User != null && vc.Car.User.FirstName != null && vc.Car.User.FirstName.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.User != null && vc.Car.User.LastName != null && vc.Car.User.LastName.Contains(searchTerm)));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusCode))
            {
                query = query.Where(vc => vc.StatusCode == statusCode);
            }

            // Apply date filter
            if (fromDate.HasValue)
            {
                query = query.Where(vc => vc.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(vc => vc.CreatedAt <= toDate.Value.AddDays(1).AddTicks(-1));
            }

            return await query
                .OrderByDescending(vc => vc.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<VehicleCheckin>> GetByCarIdAsync(long carId)
        {
            return await _context.VehicleCheckins
                .Include(vc => vc.Car)
                .Include(vc => vc.VehicleCheckinImages)
                .Where(vc => vc.CarId == carId)
                .OrderByDescending(vc => vc.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<VehicleCheckin>> GetByMaintenanceRequestIdAsync(long maintenanceRequestId)
        {
            // MaintenanceRequest entity does not exist - return empty list
            return new List<VehicleCheckin>();
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var vehicleCheckin = await _context.VehicleCheckins.FindAsync(id);
            if (vehicleCheckin == null)
                return false;

            _context.VehicleCheckins.Remove(vehicleCheckin);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<VehicleCheckin?> GetByIdWithDetailsAsync(long id)
        {
            return await _context.VehicleCheckins
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.User)
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.VehicleType)
                .Include(vc => vc.VehicleCheckinImages)
                .FirstOrDefaultAsync(vc => vc.Id == id);
        }

        public async Task<List<VehicleCheckin>> GetAllWithDetailsAsync(int page = 1, int pageSize = 10, string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? branchId = null)
        {
            var query = _context.VehicleCheckins
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.User)
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.VehicleType)
                .Include(vc => vc.VehicleCheckinImages)
                .AsQueryable();

            // Apply branch filter
            if (branchId.HasValue)
            {
                query = query.Where(vc => vc.BranchId == branchId.Value);
                System.Diagnostics.Debug.WriteLine($"[VehicleCheckinRepository] Branch filter applied: branchId={branchId.Value}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[VehicleCheckinRepository] No branchId provided, returning all branches");
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(vc => 
                    (vc.Car != null && vc.Car.LicensePlate != null && vc.Car.LicensePlate.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.VinNumber != null && vc.Car.VinNumber.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.User != null && vc.Car.User.FirstName != null && vc.Car.User.FirstName.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.User != null && vc.Car.User.LastName != null && vc.Car.User.LastName.Contains(searchTerm)));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusCode))
            {
                query = query.Where(vc => vc.StatusCode == statusCode);
            }

            // Apply date filter
            if (fromDate.HasValue)
            {
                query = query.Where(vc => vc.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(vc => vc.CreatedAt <= toDate.Value.AddDays(1).AddTicks(-1));
            }

            return await query
                .OrderByDescending(vc => vc.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? branchId = null)
        {
            var query = _context.VehicleCheckins.AsQueryable();

            // Apply branch filter
            if (branchId.HasValue)
            {
                query = query.Where(vc => vc.BranchId == branchId.Value);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(vc => 
                    (vc.Car != null && vc.Car.LicensePlate != null && vc.Car.LicensePlate.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.VinNumber != null && vc.Car.VinNumber.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.User != null && vc.Car.User.FirstName != null && vc.Car.User.FirstName.Contains(searchTerm)) ||
                    (vc.Car != null && vc.Car.User != null && vc.Car.User.LastName != null && vc.Car.User.LastName.Contains(searchTerm)));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusCode))
            {
                query = query.Where(vc => vc.StatusCode == statusCode);
            }

            // Apply date filter
            if (fromDate.HasValue)
            {
                query = query.Where(vc => vc.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(vc => vc.CreatedAt <= toDate.Value.AddDays(1).AddTicks(-1));
            }

            return await query.CountAsync();
        }

        public async Task<List<Car>> SearchCarsAsync(string searchTerm)
        {
            return await _context.Cars
                .Include(c => c.User)
                .Include(c => c.Branch)
                .Include(c => c.VehicleType)
                .Where(c => c.LicensePlate.Contains(searchTerm) || 
                           (c.VinNumber != null && c.VinNumber.Contains(searchTerm)))
                .ToListAsync();
        }
    }
}
