using AutoMapper;
using BE.DTOs.CarOfAutoOwner;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using System.Text.RegularExpressions;

namespace BE.services
{
    public class CarOfAutoOwnerService : ICarOfAutoOwnerService
    {
        private readonly ICarOfAutoOwnerRepository _repository;
        private readonly IMapper _mapper;
        // Biển số: ví dụ 30A-12345, 30A-123.45, 59N-12345
        private static readonly Regex LicensePlateRegex = new(@"^(?:[0-9]{2}[A-Z]-[0-9]{2}\.[0-9]{3}|[0-9]{2}[A-Z]-[0-9]{3}\.[0-9]{2}|[0-9]{2}[A-Z]-?[0-9]{4,5})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VinRegex = new(@"^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EngineRegex = new(@"^[A-Z0-9\-]{5,20}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public CarOfAutoOwnerService(ICarOfAutoOwnerRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<ResponseDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var cars = await _repository.GetAllAsync(page, pageSize);
            return _mapper.Map<List<ResponseDto>>(cars);
        }

        public async Task<ResponseDto?> GetByIdAsync(long id)
        {
            var car = await _repository.GetByIdAsync(id);
            return _mapper.Map<ResponseDto?>(car);
        }

        public async Task<List<ResponseDto>> GetByUserIdAsync(long userId)
        {
            var cars = await _repository.GetByUserIdAsync(userId);
            var result = _mapper.Map<List<ResponseDto>>(cars);
            
            // Đảm bảo VehicleTypeName được set đúng
            for (int i = 0; i < result.Count; i++)
            {
                if (string.IsNullOrEmpty(result[i].VehicleTypeName) && cars[i].VehicleType != null)
                {
                    result[i].VehicleTypeName = cars[i].VehicleType.Name;
                }
            }
            
            return result;
        }

        public async Task<List<ResponseDto>> GetServicedCarsByUserIdAsync(long userId)
        {
            var cars = await _repository.GetServicedCarsByUserIdAsync(userId);
            return _mapper.Map<List<ResponseDto>>(cars);
        }

        public async Task<ResponseDto> CreateAsync(RequestDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto), "Request DTO cannot be null.");
            }

            await NormalizeAndValidateAsync(dto);

            var car = _mapper.Map<Car>(dto);
            car.CreatedDate = DateTime.UtcNow;
            // Xe của khách hàng không cần chi nhánh
            car.BranchId = null;

            await _repository.CreateAsync(car);
            return _mapper.Map<ResponseDto>(car);
        }

        public async Task<ResponseDto> UpdateAsync(long id, RequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Car not found.");

            await NormalizeAndValidateAsync(dto, existingCarId: id, fallbackUserId: existing.UserId);

            _mapper.Map(dto, existing);
            existing.LastModifiedDate = DateTime.UtcNow;
            // Xe của khách hàng không cần chi nhánh
            existing.BranchId = null;

            await _repository.UpdateAsync(existing);
            return _mapper.Map<ResponseDto>(existing);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<DuplicateCheckResponseDto> CheckDuplicateAsync(string? licensePlate = null, string? vinNumber = null, string? engineNumber = null, long? excludeCarId = null)
        {
            var normalizedPlate = licensePlate?.Trim().ToUpperInvariant();
            var normalizedVin = vinNumber?.Trim().ToUpperInvariant();
            var normalizedEngine = engineNumber?.Trim().ToUpperInvariant();

            var response = new DuplicateCheckResponseDto();

            if (!string.IsNullOrEmpty(normalizedPlate))
            {
                response.LicensePlateExists = await _repository.ExistsLicensePlateAsync(normalizedPlate, excludeCarId);
            }

            if (!string.IsNullOrEmpty(normalizedVin))
            {
                response.VinNumberExists = await _repository.ExistsVinNumberAsync(normalizedVin, excludeCarId);
            }

            if (!string.IsNullOrEmpty(normalizedEngine))
            {
                response.EngineNumberExists = await _repository.ExistsEngineNumberAsync(normalizedEngine, excludeCarId);
            }

            return response;
        }

        private async Task NormalizeAndValidateAsync(RequestDto dto, long? existingCarId = null, long? fallbackUserId = null)
        {
            dto.CarName = dto.CarName?.Trim();
            if (string.IsNullOrWhiteSpace(dto.CarName))
                throw new ArgumentException("Tên xe là bắt buộc.");
            if (dto.CarName!.Length > 100)
                throw new ArgumentException("Tên xe không được vượt quá 100 ký tự.");

            dto.CarModel = dto.CarModel?.Trim();
            if (!string.IsNullOrEmpty(dto.CarModel) && dto.CarModel.Length > 100)
                throw new ArgumentException("Mẫu xe không được vượt quá 100 ký tự.");

            dto.Color = dto.Color?.Trim();
            if (!string.IsNullOrEmpty(dto.Color) && dto.Color.Length > 50)
                throw new ArgumentException("Màu xe không được vượt quá 50 ký tự.");

            dto.LicensePlate = dto.LicensePlate?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(dto.LicensePlate))
                throw new ArgumentException("Biển số là bắt buộc.");
            if (!LicensePlateRegex.IsMatch(dto.LicensePlate))
                throw new ArgumentException("Biển số không hợp lệ. Ví dụ hợp lệ: 30A-123.45, 30A-12345, 59N1-12345.");

            dto.VehicleEngineNumber = dto.VehicleEngineNumber?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(dto.VehicleEngineNumber) && !EngineRegex.IsMatch(dto.VehicleEngineNumber))
                throw new ArgumentException("Số máy không hợp lệ. Chỉ cho phép 5-20 ký tự chữ số hoặc dấu gạch ngang.");

            dto.VinNumber = dto.VinNumber?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(dto.VinNumber) && !VinRegex.IsMatch(dto.VinNumber))
                throw new ArgumentException("Số khung (VIN) phải gồm đúng 17 ký tự chữ số/ chữ cái (không bao gồm I, O, Q).");

            // Cho phép null VehicleTypeId khi khách hàng tự thêm xe (thông tin cơ bản)
            // Consultant có thể cập nhật sau khi check-in
            if (dto.VehicleTypeId.HasValue && dto.VehicleTypeId.Value <= 0)
                throw new ArgumentException("Loại xe không hợp lệ.");

            var year = dto.YearOfManufacture;
            if (year.HasValue)
            {
                var currentYear = DateTime.UtcNow.Year + 1;
                if (year < 1900 || year > currentYear)
                    throw new ArgumentException($"Năm sản xuất phải nằm trong khoảng 1900 - {currentYear}.");
            }

            var userId = dto.UserId ?? fallbackUserId;
            if (!userId.HasValue || userId.Value <= 0)
                throw new ArgumentException("Khách hàng sở hữu xe không hợp lệ.");
            dto.UserId = userId;

            // Check duplicates
            if (await _repository.ExistsLicensePlateAsync(dto.LicensePlate!, existingCarId))
                throw new ArgumentException("Biển số này đã được đăng ký cho một xe khác.");

            if (!string.IsNullOrEmpty(dto.VinNumber) && await _repository.ExistsVinNumberAsync(dto.VinNumber, existingCarId))
                throw new ArgumentException("Số khung (VIN) này đã tồn tại trong hệ thống.");

            if (!string.IsNullOrEmpty(dto.VehicleEngineNumber) && await _repository.ExistsEngineNumberAsync(dto.VehicleEngineNumber, existingCarId))
                throw new ArgumentException("Số máy này đã tồn tại trong hệ thống.");
        }
    }
}
