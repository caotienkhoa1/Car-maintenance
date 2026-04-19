using BE.DTOs.VehicleCheckin;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.services
{
    public class VehicleCheckinService : IVehicleCheckinService
    {
        private readonly IVehicleCheckinRepository _vehicleCheckinRepository;
        private readonly CarMaintenanceDbContext _context;

        public VehicleCheckinService(IVehicleCheckinRepository vehicleCheckinRepository, CarMaintenanceDbContext context)
        {
            _vehicleCheckinRepository = vehicleCheckinRepository;
            _context = context;
        }

        public async Task<ResponseDto> CreateVehicleCheckinAsync(VehicleCheckinRequestDto request, long? createdByUserId = null)
        {
            var car = await _context.Cars
                .Include(c => c.User)
                .Include(c => c.Branch)
                .Include(c => c.VehicleType)
                .FirstOrDefaultAsync(c => c.Id == request.CarId);

            if (car == null)
            {
                throw new ArgumentException("Car not found");
            }

            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId);
            
            // Lấy thông tin người tạo (consulter) nếu có userId
            string? consulterName = null;
            if (createdByUserId.HasValue)
            {
                var employee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == createdByUserId.Value);
                if (employee != null)
                {
                    consulterName = $"{employee.FirstName} {employee.LastName}".Trim();
                }
            }

            var overrideLicensePlate = string.IsNullOrWhiteSpace(request.LicensePlate)
                ? car.LicensePlate
                : request.LicensePlate.Trim();
            var overrideVin = string.IsNullOrWhiteSpace(request.VinNumber)
                ? car.VinNumber
                : request.VinNumber.Trim();
            var overrideEngineNumber = string.IsNullOrWhiteSpace(request.EngineNumber)
                ? car.VehicleEngineNumber
                : request.EngineNumber.Trim();
            var overrideCarName = string.IsNullOrWhiteSpace(request.CarName)
                ? car.CarName
                : request.CarName.Trim();
            var overrideCarModel = string.IsNullOrWhiteSpace(request.CarModel)
                ? car.CarModel
                : request.CarModel.Trim();
            var overrideVehicleTypeName = string.IsNullOrWhiteSpace(request.VehicleTypeName)
                ? car.VehicleType?.Name
                : request.VehicleTypeName.Trim();
            var overrideVehicleTypeId = request.VehicleTypeId ?? car.VehicleTypeId;
            var overrideCarColor = string.IsNullOrWhiteSpace(request.CarColor)
                ? car.Color
                : request.CarColor.Trim();
            var overrideYearOfManufacture = request.YearOfManufacture ?? car.YearOfManufacture;

            string? defaultCustomerName = null;
            if (car.User != null)
            {
                var firstName = car.User.FirstName?.Trim();
                var lastName = car.User.LastName?.Trim();
                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                {
                    defaultCustomerName = $"{firstName} {lastName}";
                }
                else if (!string.IsNullOrWhiteSpace(firstName))
                {
                    defaultCustomerName = firstName;
                }
                else if (!string.IsNullOrWhiteSpace(lastName))
                {
                    defaultCustomerName = lastName;
                }
            }

            var overrideCustomerName = string.IsNullOrWhiteSpace(request.CustomerName)
                ? defaultCustomerName
                : request.CustomerName.Trim();
            var overrideCustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone)
                ? car.User?.Phone
                : request.CustomerPhone.Trim();
            var overrideCustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail)
                ? car.User?.Email
                : request.CustomerEmail.Trim();
            var overrideCustomerAddress = string.IsNullOrWhiteSpace(request.CustomerAddress)
                ? car.User?.Address
                : request.CustomerAddress.Trim();

            var vehicleCheckin = new VehicleCheckin
            {
                CarId = request.CarId,
                Mileage = request.Mileage,
                Notes = request.Notes,
                BranchId = request.BranchId,
                Code = await GenerateVehicleCheckinCodeAsync(),
                StatusCode = "PENDING", // Default status - chỉ có PENDING và CONFIRMED
                CreatedAt = DateTime.UtcNow,
                VehicleCheckinImages = request.ImageUrls.Select((url, index) => new VehicleCheckinImage
                {
                    ImageUrl = url,
                    CreatedAt = DateTime.UtcNow,
                    Description = request.ImageDescriptions != null && index < request.ImageDescriptions.Count 
                        ? request.ImageDescriptions[index] 
                        : null,
                    Category = request.ImageCategories != null && index < request.ImageCategories.Count 
                        ? request.ImageCategories[index] 
                        : null
                }).ToList(),
                SnapshotCarName = overrideCarName,
                SnapshotCarModel = overrideCarModel,
                SnapshotVehicleType = overrideVehicleTypeName,
                SnapshotVehicleTypeId = overrideVehicleTypeId,
                SnapshotLicensePlate = overrideLicensePlate,
                SnapshotVinNumber = overrideVin,
                SnapshotEngineNumber = overrideEngineNumber,
                SnapshotYearOfManufacture = overrideYearOfManufacture,
                SnapshotColor = overrideCarColor,
                SnapshotMileage = request.Mileage,
                SnapshotCustomerName = overrideCustomerName,
                SnapshotCustomerPhone = overrideCustomerPhone,
                SnapshotCustomerEmail = overrideCustomerEmail,
                SnapshotCustomerAddress = overrideCustomerAddress,
                SnapshotBranchName = branch?.Name ?? car.Branch?.Name,
                SnapshotConsulterName = consulterName
            };

            var createdVehicleCheckin = await _vehicleCheckinRepository.CreateAsync(vehicleCheckin);
            return await GetVehicleCheckinByIdAsync(createdVehicleCheckin.Id);
        }

        public async Task<ResponseDto> UpdateVehicleCheckinAsync(UpdateDto request)
        {
            var existingVehicleCheckin = await _vehicleCheckinRepository.GetByIdAsync(request.Id);
            if (existingVehicleCheckin == null)
                throw new ArgumentException("Vehicle check-in not found");

            existingVehicleCheckin.Mileage = request.Mileage;
            existingVehicleCheckin.Notes = request.Notes;
            existingVehicleCheckin.SnapshotMileage = request.Mileage;

            // Update images if provided
            if (request.ImageUrls.Any())
            {
                // Remove existing images
                existingVehicleCheckin.VehicleCheckinImages.Clear();
                
                // Add new images
                existingVehicleCheckin.VehicleCheckinImages = request.ImageUrls.Select((url, index) => new VehicleCheckinImage
                {
                    ImageUrl = url,
                    CreatedAt = DateTime.UtcNow,
                    Description = request.ImageDescriptions != null && index < request.ImageDescriptions.Count 
                        ? request.ImageDescriptions[index] 
                        : null,
                    Category = request.ImageCategories != null && index < request.ImageCategories.Count 
                        ? request.ImageCategories[index] 
                        : null
                }).ToList();
            }

            await _vehicleCheckinRepository.UpdateAsync(existingVehicleCheckin);
            return await GetVehicleCheckinByIdAsync(existingVehicleCheckin.Id);
        }

        public async Task<ResponseDto> GetVehicleCheckinByIdAsync(long id)
        {
            var vehicleCheckin = await _vehicleCheckinRepository.GetByIdWithDetailsAsync(id);
            if (vehicleCheckin == null)
                throw new ArgumentException("Vehicle check-in not found");

            return await MapToResponseDTO(vehicleCheckin);
        }

        public async Task<List<ListResponseDto>> GetAllVehicleCheckinsAsync(int page = 1, int pageSize = 10, string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? userId = null, long? branchId = null)
        {
            // ✅ Tự động lấy BranchId của user đang đăng nhập nếu chưa có
            if (!branchId.HasValue && userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null && user.BranchId.HasValue)
                {
                    branchId = user.BranchId;
                }
            }

            var vehicleCheckins = await _vehicleCheckinRepository.GetAllWithDetailsAsync(page, pageSize, searchTerm, statusCode, fromDate, toDate, branchId);
            return vehicleCheckins.Select(MapToListResponseDTO).ToList();
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null, long? userId = null, long? branchId = null)
        {
            // ✅ Tự động lấy BranchId của user đang đăng nhập nếu chưa có
            if (!branchId.HasValue && userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null && user.BranchId.HasValue)
                {
                    branchId = user.BranchId;
                }
            }

            return await _vehicleCheckinRepository.GetTotalCountAsync(searchTerm, statusCode, fromDate, toDate, branchId);
        }

        public async Task<List<ListResponseDto>> GetVehicleCheckinsByCarIdAsync(long carId)
        {
            var vehicleCheckins = await _vehicleCheckinRepository.GetByCarIdAsync(carId);
            return vehicleCheckins.Select(MapToListResponseDTO).ToList();
        }

        public async Task<List<ListResponseDto>> GetVehicleCheckinsByMaintenanceRequestIdAsync(long maintenanceRequestId)
        {
            var vehicleCheckins = await _vehicleCheckinRepository.GetByMaintenanceRequestIdAsync(maintenanceRequestId);
            return vehicleCheckins.Select(MapToListResponseDTO).ToList();
        }

        public async Task<bool> DeleteVehicleCheckinAsync(long id)
        {
            return await _vehicleCheckinRepository.DeleteAsync(id);
        }

        private async Task<ResponseDto> MapToResponseDTO(VehicleCheckin vehicleCheckin)
        {
            // Get branch name as fallback
            var branchId = vehicleCheckin.BranchId ?? vehicleCheckin.Car?.BranchId;
            var branchName = vehicleCheckin.Branch?.Name ?? vehicleCheckin.Car?.Branch?.Name;
            if (string.IsNullOrEmpty(branchName) && branchId.HasValue)
            {
                branchName = await GetBranchNameById(branchId);
            }
            
            return new ResponseDto
            {
                Id = vehicleCheckin.Id,
                CarId = vehicleCheckin.CarId ?? 0,
                MaintenanceRequestId = 0, // Not supported - MaintenanceRequest entity does not exist
                Mileage = vehicleCheckin.Mileage ?? 0,
                Notes = vehicleCheckin.Notes,
                CreatedAt = vehicleCheckin.CreatedAt,
                Code = vehicleCheckin.Code,
                
                // Car information (snapshot preferred)
                CarName = vehicleCheckin.SnapshotCarName ?? vehicleCheckin.Car?.CarName,
                CarModel = vehicleCheckin.SnapshotCarModel ?? vehicleCheckin.Car?.CarModel,
                LicensePlate = vehicleCheckin.SnapshotLicensePlate ?? vehicleCheckin.Car?.LicensePlate,
                VinNumber = vehicleCheckin.SnapshotVinNumber ?? vehicleCheckin.Car?.VinNumber,
                VehicleEngineNumber = vehicleCheckin.SnapshotEngineNumber ?? vehicleCheckin.Car?.VehicleEngineNumber,
                Color = vehicleCheckin.SnapshotColor ?? vehicleCheckin.Car?.Color,
                YearOfManufacture = vehicleCheckin.SnapshotYearOfManufacture ?? vehicleCheckin.Car?.YearOfManufacture,
                VehicleType = vehicleCheckin.SnapshotVehicleType ?? vehicleCheckin.Car?.VehicleType?.Name,
                VehicleTypeId = vehicleCheckin.SnapshotVehicleTypeId ?? vehicleCheckin.Car?.VehicleTypeId,
                
                // Customer information
                CustomerName = vehicleCheckin.SnapshotCustomerName ?? $"{vehicleCheckin.Car?.User?.FirstName} {vehicleCheckin.Car?.User?.LastName}".Trim(),
                CustomerPhone = vehicleCheckin.SnapshotCustomerPhone ?? vehicleCheckin.Car?.User?.Phone,
                CustomerEmail = vehicleCheckin.SnapshotCustomerEmail ?? vehicleCheckin.Car?.User?.Email,
                CustomerAddress = vehicleCheckin.SnapshotCustomerAddress ?? vehicleCheckin.Car?.User?.Address,
                
                // Branch information
                BranchId = branchId,
                BranchName = vehicleCheckin.SnapshotBranchName ?? branchName,
                
                // Consulter information (người tạo)
                ConsulterName = vehicleCheckin.SnapshotConsulterName,
                
                // Images
                Images = vehicleCheckin.VehicleCheckinImages?.Select(img => new VehicleCheckinImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    CreatedAt = img.CreatedAt,
                    Description = img.Description,
                    Category = img.Category
                }).ToList() ?? new List<VehicleCheckinImageDto>(),
                
                // Maintenance request info - Not supported
                MaintenanceRequestStatus = null,
                RequestDate = null,
                
                // VehicleCheckin status
                StatusCode = vehicleCheckin.StatusCode
            };
        }

        public async Task<ResponseDto> LinkMaintenanceRequestAsync(long vehicleCheckinId, long maintenanceRequestId)
        {
            // MaintenanceRequest entity does not exist - this method is not supported
            throw new NotImplementedException("MaintenanceRequest entity does not exist in the database schema");
        }

        public async Task<object> SearchVehicleAsync(string searchTerm)
        {
            // Tìm kiếm xe theo biển số hoặc số khung
            var cars = await _vehicleCheckinRepository.SearchCarsAsync(searchTerm);
            
            if (!cars.Any())
            {
                return new { found = false, message = "Không tìm thấy xe trong hệ thống" };
            }

            // Nếu tìm thấy xe, trả về thông tin xe
            var car = cars.First();
            return new 
            { 
                found = true,
                car = new
                {
                    id = car.Id,
                    carName = car.CarName,
                    carModel = car.CarModel,
                    licensePlate = car.LicensePlate,
                    vinNumber = car.VinNumber,
                    color = car.Color,
                    yearOfManufacture = car.YearOfManufacture,
                    vehicleEngineNumber = car.VehicleEngineNumber,
                    customerName = $"{car.User?.FirstName} {car.User?.LastName}".Trim(),
                    customerPhone = car.User?.Phone,
                    customerEmail = car.User?.Email,
                    branchName = car.Branch?.Name
                }
            };
        }

        private ListResponseDto MapToListResponseDTO(VehicleCheckin vehicleCheckin)
        {
            return new ListResponseDto
            {
                Id = vehicleCheckin.Id,
                CarId = vehicleCheckin.CarId ?? 0,
                Code = vehicleCheckin.Code,
                CarName = vehicleCheckin.SnapshotCarName ?? vehicleCheckin.Car?.CarName,
                LicensePlate = vehicleCheckin.SnapshotLicensePlate ?? vehicleCheckin.Car?.LicensePlate,
                VinNumber = vehicleCheckin.SnapshotVinNumber ?? vehicleCheckin.Car?.VinNumber,
                CustomerName = vehicleCheckin.SnapshotCustomerName ?? $"{vehicleCheckin.Car?.User?.FirstName} {vehicleCheckin.Car?.User?.LastName}".Trim(),
                VehicleType = vehicleCheckin.SnapshotVehicleType ?? vehicleCheckin.Car?.VehicleType?.Name,
                Mileage = vehicleCheckin.Mileage ?? 0,
                CreatedAt = vehicleCheckin.CreatedAt,
                Notes = vehicleCheckin.Notes,
                FirstImageUrl = vehicleCheckin.VehicleCheckinImages?.FirstOrDefault()?.ImageUrl,
                BranchId = vehicleCheckin.BranchId ?? vehicleCheckin.Car?.BranchId,
                BranchName = vehicleCheckin.SnapshotBranchName ?? vehicleCheckin.Branch?.Name ?? vehicleCheckin.Car?.Branch?.Name,
                MaintenanceRequestStatus = null, // Not supported - MaintenanceRequest entity does not exist
                StatusCode = vehicleCheckin.StatusCode
            };
        }

        private async Task<string?> GetBranchNameById(long? branchId)
        {
            if (!branchId.HasValue) return null;
            
            try
            {
                var branch = await _context.Branches.FindAsync(branchId.Value);
                return branch?.Name;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> GenerateVehicleCheckinCodeAsync()
        {
            string code;
            bool isUnique;
            
            do
            {
                // Tạo code theo format VCI-xxxxx (5 số)
                var random = new Random();
                var number = random.Next(10000, 99999); // 5 số từ 10000-99999
                code = $"VCI-{number}";
                
                // Kiểm tra code có trùng không
                isUnique = !await _context.VehicleCheckins.AnyAsync(vc => vc.Code == code);
            } while (!isUnique);
            
            return code;
        }

        public async Task<ResponseDto> UpdateStatusAsync(long id, string statusCode)
        {
            var vehicleCheckin = await _context.VehicleCheckins
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.User)
                .Include(vc => vc.Car)
                    .ThenInclude(c => c.Branch)
                .Include(vc => vc.Branch)
                .Include(vc => vc.VehicleCheckinImages)
                .FirstOrDefaultAsync(vc => vc.Id == id);

            if (vehicleCheckin == null)
            {
                throw new ArgumentException("Vehicle check-in not found");
            }

            // Validate status
            if (statusCode != "PENDING" && statusCode != "CONFIRMED")
            {
                throw new ArgumentException("Trạng thái chỉ được phép là PENDING hoặc CONFIRMED");
            }

            vehicleCheckin.StatusCode = statusCode;
            await _context.SaveChangesAsync();

            return await MapToResponseDTO(vehicleCheckin);
        }
    }
}
