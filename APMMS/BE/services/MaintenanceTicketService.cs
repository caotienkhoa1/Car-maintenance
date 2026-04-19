using AutoMapper;

using RequestDto = BE.DTOs.MaintenanceTicket.RequestDto;
using ResponseDto = BE.DTOs.MaintenanceTicket.ResponseDto;
using ListResponseDto = BE.DTOs.MaintenanceTicket.ListResponseDto;
using CreateFromCheckinDto = BE.DTOs.MaintenanceTicket.CreateFromCheckinDto;
using UpdateStatusDto = BE.DTOs.MaintenanceTicket.UpdateStatusDto;
using AssignTechnicianDto = BE.DTOs.MaintenanceTicket.AssignTechnicianDto;
using AssignTechniciansDto = BE.DTOs.MaintenanceTicket.AssignTechniciansDto;
using TechnicianInfoDto = BE.DTOs.MaintenanceTicket.TechnicianInfoDto;
using UserMaintenanceHistoryDto = BE.DTOs.MaintenanceTicket.UserMaintenanceHistoryDto;
using TotalReceiptRequestDto = BE.DTOs.TotalReceipt.RequestDto;

using BE.DTOs.TotalReceipt;
using BE.interfaces;
using BE.repository.IRepository;
using BE.models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BE.DTOs.ServiceTask;



namespace BE.services
{
    public class MaintenanceTicketService : IMaintenanceTicketService
    {
        private readonly IMaintenanceTicketRepository _maintenanceTicketRepository;
        private readonly IVehicleCheckinRepository _vehicleCheckinRepository;
        private readonly ICarOfAutoOwnerRepository _carRepository;
        private readonly IServiceCategoryRepository _serviceCategoryRepository;
        private readonly IHistoryLogRepository _historyLogRepository;
        private readonly IServicePackageService _servicePackageService;
        private readonly ITicketComponentService _ticketComponentService;
        private readonly IServiceTaskService _serviceTaskService;
        private readonly IServiceTaskRepository _serviceTaskRepository;
        private readonly ITotalReceiptService _totalReceiptService;
        private readonly IMapper _mapper;
        private readonly CarMaintenanceDbContext _context;
        public MaintenanceTicketService(

            IMaintenanceTicketRepository maintenanceTicketRepository,

            IVehicleCheckinRepository vehicleCheckinRepository,
            ICarOfAutoOwnerRepository carRepository,
            IServiceCategoryRepository serviceCategoryRepository,
            IHistoryLogRepository historyLogRepository,
            IServicePackageService servicePackageService,
            ITicketComponentService ticketComponentService,
            IServiceTaskService serviceTaskService,
            IServiceTaskRepository serviceTaskRepository,
            ITotalReceiptService totalReceiptService,
            IMapper mapper,
            CarMaintenanceDbContext context)

        {

            _maintenanceTicketRepository = maintenanceTicketRepository;
            _vehicleCheckinRepository = vehicleCheckinRepository;
            _carRepository = carRepository;
            _serviceCategoryRepository = serviceCategoryRepository;
            _historyLogRepository = historyLogRepository;
            _servicePackageService = servicePackageService;
            _ticketComponentService = ticketComponentService;
            _serviceTaskService = serviceTaskService;
            _serviceTaskRepository = serviceTaskRepository;
            _totalReceiptService = totalReceiptService;
            _mapper = mapper;
            _context = context;

        }



        public async Task<ResponseDto> CreateMaintenanceTicketAsync(RequestDto request)

        {

            var maintenanceTicket = _mapper.Map<MaintenanceTicket>(request);

            // Tự sinh code 7 ký tự ngẫu nhiên

            maintenanceTicket.Code = await GenerateUniqueCodeAsync();

            maintenanceTicket.CreatedAt = DateTime.UtcNow;

            // Validate: Car exists (no branch restriction)
            if (maintenanceTicket.CarId.HasValue)
            {
                var carDetails = await _context.Cars
                    .Include(c => c.User)
                    .Include(c => c.Branch)
                    .Include(c => c.VehicleType)
                    .FirstOrDefaultAsync(c => c.Id == maintenanceTicket.CarId.Value);
                if (carDetails == null)
                    throw new ArgumentException("Car not found");

                maintenanceTicket.SnapshotCarName = carDetails.CarName;
                maintenanceTicket.SnapshotCarModel = carDetails.CarModel;
                maintenanceTicket.SnapshotVehicleType = carDetails.VehicleType?.Name;
                maintenanceTicket.SnapshotVehicleTypeId = carDetails.VehicleTypeId;
                maintenanceTicket.SnapshotLicensePlate = carDetails.LicensePlate;
                maintenanceTicket.SnapshotVinNumber = carDetails.VinNumber;
                maintenanceTicket.SnapshotEngineNumber = carDetails.VehicleEngineNumber;
                maintenanceTicket.SnapshotYearOfManufacture = carDetails.YearOfManufacture;
                maintenanceTicket.SnapshotColor = carDetails.Color;
                maintenanceTicket.SnapshotCustomerName = $"{carDetails.User?.FirstName} {carDetails.User?.LastName}".Trim();
                maintenanceTicket.SnapshotCustomerPhone = carDetails.User?.Phone;
                maintenanceTicket.SnapshotCustomerEmail = carDetails.User?.Email;
                maintenanceTicket.SnapshotCustomerAddress = carDetails.User?.Address;
                maintenanceTicket.SnapshotMileage = null;
                maintenanceTicket.SnapshotBranchName = carDetails.Branch?.Name;
            }

            // Validate: ServiceCategory exists if provided
            if (maintenanceTicket.ServiceCategoryId.HasValue)
            {
                var sc = await _serviceCategoryRepository.GetByIdAsync(maintenanceTicket.ServiceCategoryId.Value);
                if (sc == null)
                    throw new ArgumentException("Service category not found");
            }

            if (string.IsNullOrWhiteSpace(maintenanceTicket.PriorityLevel))
            {
                maintenanceTicket.PriorityLevel = "NORMAL";
            }
            else
            {
                maintenanceTicket.PriorityLevel = maintenanceTicket.PriorityLevel.ToUpperInvariant();
            }

            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == maintenanceTicket.BranchId);
            if (branch != null)
            {
                maintenanceTicket.SnapshotBranchName = branch.Name;
            }

            var consulter = await _context.Users.FirstOrDefaultAsync(u => u.Id == maintenanceTicket.ConsulterId);
            if (consulter != null)
            {
                maintenanceTicket.SnapshotConsulterName = $"{consulter.FirstName} {consulter.LastName}".Trim();
            }

            var createdTicket = await _maintenanceTicketRepository.CreateAsync(maintenanceTicket);
            
            // ✅ Load lại với ServicePackage để map đầy đủ
            var fullCreatedTicket = await _maintenanceTicketRepository.GetByIdAsync(createdTicket.Id);
            return MapToResponseWithServicePackage(fullCreatedTicket);

        }



        public async Task<ResponseDto> CreateFromVehicleCheckinAsync(CreateFromCheckinDto request)

        {

            var vehicleCheckin = await _vehicleCheckinRepository.GetByIdAsync(request.VehicleCheckinId);

            if (vehicleCheckin == null)

                throw new ArgumentException("Vehicle check-in not found");

            // Note: MaintenanceRequestId check removed - MaintenanceRequest entity does not exist



            var maintenanceTicket = new MaintenanceTicket

            {

                VehicleCheckinId = request.VehicleCheckinId,

                CarId = vehicleCheckin.CarId,

                ConsulterId = request.ConsulterId,

                TechnicianId = null,

                BranchId = request.BranchId,

                ScheduleServiceId = request.ScheduleServiceId,

                StatusCode = "PENDING",

                PriorityLevel = string.IsNullOrWhiteSpace(request.PriorityLevel)

                    ? "NORMAL"

                    : request.PriorityLevel.ToUpperInvariant(),

                Description = request.Description,

                Code = await GenerateUniqueCodeAsync()

            };



            if (request.ServiceCategoryId.HasValue)

            {

                maintenanceTicket.ServiceCategoryId = request.ServiceCategoryId;

                var sc = await _serviceCategoryRepository.GetByIdAsync(request.ServiceCategoryId.Value);

                if (sc == null)

                    throw new ArgumentException("Service category not found");

            }



            if (maintenanceTicket.CarId.HasValue)

            {

                var carExists = await _context.Cars.AnyAsync(c => c.Id == maintenanceTicket.CarId.Value);

                if (!carExists)

                    throw new ArgumentException("Car not found");

            }



            var consulter = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ConsulterId);

            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId);

            var carEntity = vehicleCheckin.Car ?? await _context.Cars

                .Include(c => c.User)

                .Include(c => c.VehicleType)

                .Include(c => c.Branch)

                .FirstOrDefaultAsync(c => c.Id == vehicleCheckin.CarId);



            maintenanceTicket.SnapshotCarName = vehicleCheckin.SnapshotCarName ?? carEntity?.CarName;

            maintenanceTicket.SnapshotCarModel = vehicleCheckin.SnapshotCarModel ?? carEntity?.CarModel;

            maintenanceTicket.SnapshotVehicleType = vehicleCheckin.SnapshotVehicleType ?? carEntity?.VehicleType?.Name;
            maintenanceTicket.SnapshotVehicleTypeId = vehicleCheckin.SnapshotVehicleTypeId
                ?? (carEntity?.VehicleTypeId);
            maintenanceTicket.SnapshotLicensePlate = vehicleCheckin.SnapshotLicensePlate ?? carEntity?.LicensePlate;
            maintenanceTicket.SnapshotVinNumber = vehicleCheckin.SnapshotVinNumber ?? carEntity?.VinNumber;
            maintenanceTicket.SnapshotEngineNumber = vehicleCheckin.SnapshotEngineNumber ?? carEntity?.VehicleEngineNumber;
            maintenanceTicket.SnapshotYearOfManufacture = vehicleCheckin.SnapshotYearOfManufacture ?? carEntity?.YearOfManufacture;
            maintenanceTicket.SnapshotColor = vehicleCheckin.SnapshotColor ?? carEntity?.Color;
            maintenanceTicket.SnapshotMileage = vehicleCheckin.SnapshotMileage ?? vehicleCheckin.Mileage;
            maintenanceTicket.SnapshotCustomerName = vehicleCheckin.SnapshotCustomerName
                ?? ($"{carEntity?.User?.FirstName} {carEntity?.User?.LastName}").Trim();
            maintenanceTicket.SnapshotCustomerPhone = vehicleCheckin.SnapshotCustomerPhone ?? carEntity?.User?.Phone;
            maintenanceTicket.SnapshotCustomerEmail = vehicleCheckin.SnapshotCustomerEmail ?? carEntity?.User?.Email;
            maintenanceTicket.SnapshotCustomerAddress = vehicleCheckin.SnapshotCustomerAddress ?? carEntity?.User?.Address;
            maintenanceTicket.SnapshotBranchName = vehicleCheckin.SnapshotBranchName ?? branch?.Name ?? carEntity?.Branch?.Name;
            maintenanceTicket.SnapshotConsulterName = consulter != null ? ($"{consulter.FirstName} {consulter.LastName}").Trim() : vehicleCheckin.SnapshotConsulterName;



            maintenanceTicket.CreatedAt = DateTime.UtcNow;



            var createdTicket = await _maintenanceTicketRepository.CreateAsync(maintenanceTicket);



            var consulterName = createdTicket.Consulter != null

                ? ($"{createdTicket.Consulter.FirstName} {createdTicket.Consulter.LastName}").Trim()

                : "Unknown";



            await CreateHistoryLogAsync(

                userId: request.ConsulterId,

                action: "CREATE_TICKET",

                maintenanceTicketId: createdTicket.Id,

                oldData: null,

                newData: $"Phiếu bảo dưỡng được tạo bởi {consulterName} lúc {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}"

            );



            var fullTicket = await _maintenanceTicketRepository.GetByIdAsync(createdTicket.Id);


            
            var response = new ResponseDto

            {

                Id = fullTicket.Id,

                CarId = fullTicket.CarId,

                ConsulterId = fullTicket.ConsulterId,

                TechnicianId = fullTicket.TechnicianId,

                BranchId = fullTicket.BranchId,

                StatusCode = fullTicket.StatusCode,

                ScheduleServiceId = fullTicket.ScheduleServiceId,

                Code = fullTicket.Code,

                TotalEstimatedCost = fullTicket.TotalEstimatedCost,

                CreatedDate = fullTicket.CreatedAt,

                StartTime = fullTicket.StartTime,

                EndTime = fullTicket.EndTime,

                PriorityLevel = fullTicket.PriorityLevel,

                Description = fullTicket.Description,

                CarName = fullTicket.SnapshotCarName ?? fullTicket.Car?.CarName,

                ConsulterName = fullTicket.SnapshotConsulterName ?? (fullTicket.Consulter != null ? ($"{fullTicket.Consulter.FirstName} {fullTicket.Consulter.LastName}").Trim() : null),

                TechnicianName = fullTicket.Technician != null ? ($"{fullTicket.Technician.FirstName} {fullTicket.Technician.LastName}").Trim() : null,

                BranchName = fullTicket.SnapshotBranchName ?? fullTicket.Branch?.Name,

                ScheduleServiceName = fullTicket.ScheduleService != null ? fullTicket.ScheduleService.ScheduledDate.ToString("dd/MM/yyyy") : null,

                CustomerName = fullTicket.SnapshotCustomerName ?? (fullTicket.Car?.User != null ? ($"{fullTicket.Car.User.FirstName} {fullTicket.Car.User.LastName}").Trim() : null),

                CustomerPhone = fullTicket.SnapshotCustomerPhone ?? fullTicket.Car?.User?.Phone,

                CustomerEmail = fullTicket.SnapshotCustomerEmail ?? fullTicket.Car?.User?.Email,

                CustomerAddress = fullTicket.SnapshotCustomerAddress ?? fullTicket.Car?.User?.Address,

                LicensePlate = fullTicket.SnapshotLicensePlate ?? fullTicket.Car?.LicensePlate,

                CarModel = fullTicket.SnapshotCarModel ?? fullTicket.Car?.CarModel,

                VinNumber = fullTicket.SnapshotVinNumber ?? fullTicket.Car?.VinNumber,

                VehicleEngineNumber = fullTicket.SnapshotEngineNumber ?? fullTicket.Car?.VehicleEngineNumber,

                YearOfManufacture = fullTicket.SnapshotYearOfManufacture ?? fullTicket.Car?.YearOfManufacture,

                VehicleType = fullTicket.SnapshotVehicleType ?? fullTicket.Car?.VehicleType?.Name,

                VehicleTypeId = fullTicket.SnapshotVehicleTypeId ?? fullTicket.Car?.VehicleTypeId,

                Color = fullTicket.SnapshotColor ?? fullTicket.Car?.Color,

                Technicians = fullTicket.MaintenanceTicketTechnicians != null && fullTicket.MaintenanceTicketTechnicians.Count > 0

                    ? fullTicket.MaintenanceTicketTechnicians.Select(mtt => new TechnicianInfoDto

                    {

                        TechnicianId = mtt.TechnicianId,

                        TechnicianName = mtt.Technician != null ? ($"{mtt.Technician.FirstName} {mtt.Technician.LastName}").Trim() : null,

                        RoleInTicket = mtt.RoleInTicket,

                        AssignedDate = mtt.AssignedDate

                    }).ToList()

                    : new List<TechnicianInfoDto>()

            };



            response.VehicleCheckinId = vehicleCheckin.Id;

            response.Mileage = fullTicket.SnapshotMileage ?? vehicleCheckin.SnapshotMileage ?? vehicleCheckin.Mileage;

            response.CheckinNotes = vehicleCheckin.Notes;

            response.CheckinImages = vehicleCheckin.VehicleCheckinImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>();



            return response;

        }



        public async Task<ResponseDto> UpdateMaintenanceTicketAsync(long id, RequestDto request)

        {

            var existingTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (existingTicket == null)

                throw new ArgumentException("Maintenance ticket not found");



            // Lưu thông tin cũ để so sánh

            var oldDescription = existingTicket.Description;

            var oldPriority = existingTicket.PriorityLevel;



            _mapper.Map(request, existingTicket);

            var updatedTicket = await _maintenanceTicketRepository.UpdateAsync(existingTicket);



            // ✅ Tạo history log để ghi nhận việc cập nhật thông tin

            var updaterName = updatedTicket.Consulter != null 

                ? ($"{updatedTicket.Consulter.FirstName} {updatedTicket.Consulter.LastName}").Trim() 

                : "Unknown";

            var changes = new List<string>();

            if (oldDescription != updatedTicket.Description) changes.Add("mô tả");

            if (oldPriority != updatedTicket.PriorityLevel) changes.Add($"mức ưu tiên: {oldPriority} → {updatedTicket.PriorityLevel}");


            
            if (changes.Count > 0)

            {

                await CreateHistoryLogAsync(

                    userId: request.ConsulterId,

                    action: "UPDATE_INFO",

                    maintenanceTicketId: id,

                    oldData: $"Description: {oldDescription}, Priority: {oldPriority}",

                    newData: $"Description: {updatedTicket.Description}, Priority: {updatedTicket.PriorityLevel}"

                );

            }

            // ✅ Load lại với ServicePackage để map đầy đủ
            var fullUpdatedTicket = await _maintenanceTicketRepository.GetByIdAsync(updatedTicket.Id);
            return MapToResponseWithServicePackage(fullUpdatedTicket);

        }



        public async Task<ResponseDto> GetMaintenanceTicketByIdAsync(long id)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");



            // ✅ Sử dụng helper method để map đầy đủ thông tin
            return MapToResponseWithServicePackage(maintenanceTicket);

        }



        public async Task<List<ListResponseDto>> GetAllMaintenanceTicketsAsync(int page = 1, int pageSize = 10, long? branchId = null)
        {
            var maintenanceTickets = await _maintenanceTicketRepository.GetAllAsync(page, pageSize, branchId);
            return _mapper.Map<List<ListResponseDto>>(maintenanceTickets);
        }



        public async Task<List<ListResponseDto>> GetMaintenanceTicketsByCarIdAsync(long carId)

        {

            var maintenanceTickets = await _maintenanceTicketRepository.GetByCarIdAsync(carId);

            return _mapper.Map<List<ListResponseDto>>(maintenanceTickets);

        }



        public async Task<List<ListResponseDto>> GetMaintenanceTicketsByStatusAsync(string statusCode)

        {

            var maintenanceTickets = await _maintenanceTicketRepository.GetByStatusAsync(statusCode);

            return _mapper.Map<List<ListResponseDto>>(maintenanceTickets);

        }



        public async Task<bool> DeleteMaintenanceTicketAsync(long id)

        {

            return await _maintenanceTicketRepository.DeleteAsync(id);

        }



        public async Task<ResponseDto> UpdateStatusAsync(long id, string statusCode)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");



            var oldStatus = maintenanceTicket.StatusCode;

            maintenanceTicket.StatusCode = statusCode;

            var updatedTicket = await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);


            
            // ✅ Tạo history log để ghi nhận việc cập nhật trạng thái

            var userName = maintenanceTicket.Consulter != null 

                ? ($"{maintenanceTicket.Consulter.FirstName} {maintenanceTicket.Consulter.LastName}").Trim() 

                : "Unknown";

            var statusMap = new Dictionary<string, string>

            {

                { "PENDING", "Chờ xử lý" },

                { "ASSIGNED", "Đã gán" },

                { "IN_PROGRESS", "Đang thực hiện" },

                { "COMPLETED", "Hoàn thành" },

                { "CANCELLED", "Đã hủy" }

            };

            var oldStatusName = statusMap.ContainsKey(oldStatus) ? statusMap[oldStatus] : oldStatus;

            var newStatusName = statusMap.ContainsKey(statusCode) ? statusMap[statusCode] : statusCode;


            
            await CreateHistoryLogAsync(

                userId: maintenanceTicket.ConsulterId,

                action: "UPDATE_STATUS",

                maintenanceTicketId: id,

                oldData: $"Status: {oldStatus}",

                newData: $"Cập nhật trạng thái: '{oldStatusName}' → '{newStatusName}' bởi {userName}"

            );

            // ✅ Load lại với ServicePackage để map đầy đủ
            var fullUpdatedTicket = await _maintenanceTicketRepository.GetByIdAsync(updatedTicket.Id);
            return MapToResponseWithServicePackage(fullUpdatedTicket);

        }



        public async Task<ResponseDto> AssignTechnicianAsync(long id, long technicianId)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");



            maintenanceTicket.TechnicianId = technicianId;

            maintenanceTicket.StatusCode = "ASSIGNED";

            var updatedTicket = await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);
            
            // ✅ Load lại với ServicePackage để map đầy đủ
            var fullUpdatedTicket = await _maintenanceTicketRepository.GetByIdAsync(updatedTicket.Id);
            return MapToResponseWithServicePackage(fullUpdatedTicket);

        }



        public async Task<ResponseDto> AddTechniciansAsync(long id, List<long> technicianIds, long? primaryId)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");



            // ✅ VALIDATION: Chỉ cho phép gán/cập nhật kỹ thuật viên khi phiếu ở trạng thái PENDING, ASSIGNED hoặc IN_PROGRESS

            if (maintenanceTicket.StatusCode != "PENDING" && maintenanceTicket.StatusCode != "ASSIGNED" && maintenanceTicket.StatusCode != "IN_PROGRESS")

                throw new ArgumentException($"Không thể gán/cập nhật kỹ thuật viên cho phiếu có trạng thái {maintenanceTicket.StatusCode}. Chỉ có thể gán khi phiếu ở trạng thái PENDING, ASSIGNED hoặc IN_PROGRESS.");



            // Cho phép danh sách rỗng (để xóa tất cả)

            // Không validate bắt buộc phải có ít nhất 1 kỹ thuật viên


            
            // Lưu trạng thái cũ để ghi log

            var oldStatus = maintenanceTicket.StatusCode;


            
            // Lấy danh sách kỹ thuật viên hiện tại và loại bỏ bản ghi trùng kỹ thuật viên (nếu có)
            var existingTechnicians = maintenanceTicket.MaintenanceTicketTechnicians?.ToList() ?? new List<MaintenanceTicketTechnician>();
            var deduped = existingTechnicians
                .GroupBy(x => x.TechnicianId)
                .Select(g => g.First())
                .ToList();
            if (deduped.Count != existingTechnicians.Count)
            {
                maintenanceTicket.MaintenanceTicketTechnicians.Clear();
                foreach (var item in deduped)
                {
                    maintenanceTicket.MaintenanceTicketTechnicians.Add(item);
                }
            }

            var existingIds = maintenanceTicket.MaintenanceTicketTechnicians.Select(x => x.TechnicianId).ToHashSet();


            
            // Xóa những người không còn trong danh sách mới

            var toRemove = existingTechnicians.Where(e => !technicianIds.Contains(e.TechnicianId)).ToList();

            if (toRemove.Count > 0)

            {

                foreach (var removeItem in toRemove)

                {

                    maintenanceTicket.MaintenanceTicketTechnicians.Remove(removeItem);

                }

            }



            // Thêm các kỹ thuật viên mới (chưa có trong danh sách)

            foreach (var techId in technicianIds.Distinct())

            {

                if (!existingIds.Contains(techId))

                {

                    maintenanceTicket.MaintenanceTicketTechnicians.Add(new MaintenanceTicketTechnician

                    {

                        MaintenanceTicketId = id,

                        TechnicianId = techId,

                        AssignedDate = DateTime.UtcNow,

                        RoleInTicket = (primaryId.HasValue && primaryId.Value == techId) ? "PRIMARY" : "ASSISTANT"

                    });

                }

            }


            
            // Cập nhật role cho các bản ghi đã tồn tại nếu có primaryId

            if (primaryId.HasValue)

            {

                foreach (var mtt in maintenanceTicket.MaintenanceTicketTechnicians.Where(x => x.MaintenanceTicketId == id))

                {

                    mtt.RoleInTicket = (mtt.TechnicianId == primaryId.Value) ? "PRIMARY" : "ASSISTANT";

                }

                // cập nhật TechnicianId chính trên ticket

                maintenanceTicket.TechnicianId = primaryId.Value;

            }

            else if (technicianIds.Count > 0)

            {

                // Nếu không có primaryId nhưng có kỹ thuật viên, chọn người đầu tiên làm chính

                maintenanceTicket.TechnicianId = technicianIds[0];

                foreach (var mtt in maintenanceTicket.MaintenanceTicketTechnicians.Where(x => x.MaintenanceTicketId == id))

                {

                    mtt.RoleInTicket = (mtt.TechnicianId == technicianIds[0]) ? "PRIMARY" : "ASSISTANT";

                }

            }

            else

            {

                // Nếu không còn kỹ thuật viên nào, xóa technician chính

                maintenanceTicket.TechnicianId = null;

            }

            // Đảm bảo chỉ tối đa 1 PRIMARY (xử lý dữ liệu trùng cũ nếu có)
            var primaries = maintenanceTicket.MaintenanceTicketTechnicians
                .Where(x => x.RoleInTicket == "PRIMARY")
                .OrderByDescending(x => x.AssignedDate)
                .ToList();
            if (primaries.Count > 1)
            {
                var keepPrimary = primaries.First();
                foreach (var p in primaries.Skip(1))
                {
                    p.RoleInTicket = "ASSISTANT";
                }
                maintenanceTicket.TechnicianId = keepPrimary.TechnicianId;
            }



            // ✅ Chuyển trạng thái: 

            // - Nếu có kỹ thuật viên và đang PENDING → ASSIGNED (KHÔNG tự động chuyển IN_PROGRESS)

            // - Nếu không còn kỹ thuật viên và đang IN_PROGRESS/ASSIGNED → PENDING

            if (technicianIds.Count > 0)

            {

                if (maintenanceTicket.StatusCode == "PENDING")

                {

                    maintenanceTicket.StatusCode = "ASSIGNED"; // ✅ Sửa: Chuyển sang ASSIGNED thay vì IN_PROGRESS

                }

            }

            else

            {

                // Không còn kỹ thuật viên, chuyển về PENDING

                if (maintenanceTicket.StatusCode == "IN_PROGRESS" || maintenanceTicket.StatusCode == "ASSIGNED")

                {

                    maintenanceTicket.StatusCode = "PENDING";

                }

            }

			await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);



            // ✅ Tạo history log để ghi nhận việc gán kỹ thuật viên

            var refreshed = await _maintenanceTicketRepository.GetByIdAsync(id);

            var technicianDetails = refreshed?.MaintenanceTicketTechnicians?.Select(mtt => 

            {

                var techName = mtt.Technician != null 

                    ? ($"{mtt.Technician.FirstName} {mtt.Technician.LastName}").Trim() 

                    : $"ID: {mtt.TechnicianId}";

                var role = mtt.RoleInTicket == "PRIMARY" ? "Chính" : "Phụ";

                return $"{techName} ({role})";

            }).ToList() ?? new List<string>();


            
            var consulterName = maintenanceTicket.Consulter != null 

                ? ($"{maintenanceTicket.Consulter.FirstName} {maintenanceTicket.Consulter.LastName}").Trim() 

                : "Unknown";
            


            foreach (var techDetail in technicianDetails)

            {

                await CreateHistoryLogAsync(

                    userId: maintenanceTicket.ConsulterId,

                    action: "ASSIGN_TECHNICIAN",

                    maintenanceTicketId: id,

                    oldData: $"Status: {oldStatus}",

                    newData: $"Gán kỹ thuật viên {techDetail} bởi {consulterName}"

                );

            }



            // Trả về chi tiết mới nhất

            var response = _mapper.Map<ResponseDto>(refreshed);

            if (refreshed?.MaintenanceTicketTechnicians != null)

            {

                response.Technicians = refreshed.MaintenanceTicketTechnicians.Select(mtt => new TechnicianInfoDto

                {

                    TechnicianId = mtt.TechnicianId,

                    TechnicianName = mtt.Technician != null ? ($"{mtt.Technician.FirstName} {mtt.Technician.LastName}").Trim() : null,

                    RoleInTicket = mtt.RoleInTicket,

                    AssignedDate = mtt.AssignedDate

                }).ToList();

            }

            // ✅ Map thông tin Service Package
            response.ServicePackageId = refreshed?.ServicePackageId;
            response.ServicePackagePrice = refreshed?.ServicePackagePrice;
            response.ServicePackageName = refreshed?.ServicePackage?.Name;

            return response;

        }



        public async Task<ResponseDto> RemoveTechniciansAsync(long id)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");



            // ✅ VALIDATION: Chỉ cho phép xóa kỹ thuật viên khi phiếu ở trạng thái PENDING, ASSIGNED hoặc IN_PROGRESS

            if (maintenanceTicket.StatusCode != "PENDING" && maintenanceTicket.StatusCode != "ASSIGNED" && maintenanceTicket.StatusCode != "IN_PROGRESS")

                throw new ArgumentException($"Không thể xóa kỹ thuật viên cho phiếu có trạng thái {maintenanceTicket.StatusCode}. Chỉ có thể xóa khi phiếu ở trạng thái PENDING, ASSIGNED hoặc IN_PROGRESS.");



            // Lưu thông tin cũ để ghi log (phải lấy trước khi xóa)

            var oldStatus = maintenanceTicket.StatusCode;

            var oldTechnicianIds = maintenanceTicket.MaintenanceTicketTechnicians?.Select(x => x.TechnicianId).ToList() ?? new List<long>();

            var oldPrimaryId = maintenanceTicket.TechnicianId;


            
            // Lấy tên kỹ thuật viên đã bị xóa (trước khi xóa)

            var removedTechnicians = new List<string>();

            if (maintenanceTicket.MaintenanceTicketTechnicians != null && maintenanceTicket.MaintenanceTicketTechnicians.Count > 0)

            {

                foreach (var mtt in maintenanceTicket.MaintenanceTicketTechnicians)

                {

                    if (mtt.Technician != null)

                    {

                        var techName = ($"{mtt.Technician.FirstName} {mtt.Technician.LastName}").Trim();

                        removedTechnicians.Add(techName);

                    }

                }

            }



            // Xóa tất cả kỹ thuật viên khỏi bảng trung gian

            if (maintenanceTicket.MaintenanceTicketTechnicians != null && maintenanceTicket.MaintenanceTicketTechnicians.Count > 0)

            {

                maintenanceTicket.MaintenanceTicketTechnicians.Clear();

            }



            // Xóa technician chính

            maintenanceTicket.TechnicianId = null;



            // ✅ Chuyển trạng thái về PENDING nếu đang ở IN_PROGRESS hoặc ASSIGNED

            if (maintenanceTicket.StatusCode == "IN_PROGRESS" || maintenanceTicket.StatusCode == "ASSIGNED")

            {

                maintenanceTicket.StatusCode = "PENDING";

            }



            await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);



            // ✅ Tạo history log để ghi nhận việc xóa kỹ thuật viên


            
            var consulterName = maintenanceTicket.Consulter != null 

                ? ($"{maintenanceTicket.Consulter.FirstName} {maintenanceTicket.Consulter.LastName}").Trim() 

                : "Unknown";
            


            foreach (var techName in removedTechnicians)

            {

                await CreateHistoryLogAsync(

                    userId: maintenanceTicket.ConsulterId,

                    action: "REMOVE_TECHNICIAN",

                    maintenanceTicketId: id,

                    oldData: $"Status: {oldStatus}",

                    newData: $"Gỡ kỹ thuật viên {techName} khỏi phiếu bởi {consulterName}"

                );

            }



            // Trả về chi tiết mới nhất

            var refreshed = await _maintenanceTicketRepository.GetByIdAsync(id);

            var response = _mapper.Map<ResponseDto>(refreshed);

            if (refreshed?.MaintenanceTicketTechnicians != null)

            {

                response.Technicians = refreshed.MaintenanceTicketTechnicians.Select(mtt => new TechnicianInfoDto

                {

                    TechnicianId = mtt.TechnicianId,

                    TechnicianName = mtt.Technician != null ? ($"{mtt.Technician.FirstName} {mtt.Technician.LastName}").Trim() : null,

                    RoleInTicket = mtt.RoleInTicket,

                    AssignedDate = mtt.AssignedDate

                }).ToList();

            }

            // ✅ Map thông tin Service Package
            response.ServicePackageId = refreshed?.ServicePackageId;
            response.ServicePackagePrice = refreshed?.ServicePackagePrice;
            response.ServicePackageName = refreshed?.ServicePackage?.Name;

            return response;

        }



        public async Task<ResponseDto> StartMaintenanceAsync(long id)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");

            // ✅ VALIDATION: Chỉ cho phép bắt đầu khi phiếu ở trạng thái ASSIGNED
            if (maintenanceTicket.StatusCode != "ASSIGNED")
                throw new ArgumentException($"Không thể bắt đầu bảo dưỡng khi phiếu ở trạng thái {maintenanceTicket.StatusCode}. Chỉ có thể bắt đầu khi phiếu ở trạng thái ASSIGNED.");

            // ✅ VALIDATION: Phải có ít nhất 1 kỹ thuật viên
            var hasTechnicians = (maintenanceTicket.TechnicianId.HasValue && maintenanceTicket.TechnicianId.Value > 0) ||
                                (maintenanceTicket.MaintenanceTicketTechnicians != null && maintenanceTicket.MaintenanceTicketTechnicians.Any());
            if (!hasTechnicians)
                throw new ArgumentException("Không thể bắt đầu bảo dưỡng khi chưa gán kỹ thuật viên.");

            var oldStatus = maintenanceTicket.StatusCode;

            maintenanceTicket.StatusCode = "IN_PROGRESS";

            maintenanceTicket.StartTime = DateTime.UtcNow;

            var updatedTicket = await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);


            
            // ✅ Tạo history log để ghi nhận việc cập nhật trạng thái

            var userName = maintenanceTicket.Consulter != null 

                ? ($"{maintenanceTicket.Consulter.FirstName} {maintenanceTicket.Consulter.LastName}").Trim() 

                : "Unknown";

            var statusMap = new Dictionary<string, string>

            {

                { "PENDING", "Chờ xử lý" },

                { "ASSIGNED", "Đã gán" },

                { "IN_PROGRESS", "Đang thực hiện" },

                { "COMPLETED", "Hoàn thành" },

                { "CANCELLED", "Đã hủy" }

            };

            var oldStatusName = statusMap.ContainsKey(oldStatus) ? statusMap[oldStatus] : oldStatus;

            var newStatusName = statusMap.ContainsKey("IN_PROGRESS") ? statusMap["IN_PROGRESS"] : "IN_PROGRESS";


            
            await CreateHistoryLogAsync(

                userId: maintenanceTicket.ConsulterId,

                action: "UPDATE_STATUS",

                maintenanceTicketId: id,

                oldData: $"Status: {oldStatus}",

                newData: $"Cập nhật trạng thái: '{oldStatusName}' → '{newStatusName}' bởi {userName}"

            );

            // ✅ Load lại với ServicePackage để map đầy đủ
            var fullUpdatedTicket = await _maintenanceTicketRepository.GetByIdAsync(updatedTicket.Id);
            return MapToResponseWithServicePackage(fullUpdatedTicket);

        }



        public async Task<ResponseDto> CompleteMaintenanceAsync(long id, long? userId = null)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");

            // ✅ VALIDATION: Kiểm tra quyền - chỉ quản lý/tư vấn viên/Admin mới được hoàn thành phiếu
            // RoleId: 1=Admin, 2=Branch Manager, 6=Consulter
            // Kỹ thuật viên (RoleId=4) KHÔNG được phép hoàn thành phiếu
            if (userId.HasValue)
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId.Value);
                
                if (user != null && user.RoleId.HasValue)
                {
                    var roleId = user.RoleId.Value;
                    // Chỉ cho phép Admin (1), Branch Manager (2), Consulter (6)
                    if (roleId != 1 && roleId != 2 && roleId != 6)
                    {
                        throw new UnauthorizedAccessException(
                            "Chỉ quản lý, tư vấn viên hoặc Admin mới được phép hoàn thành phiếu bảo dưỡng. Kỹ thuật viên không có quyền này.");
                    }
                }
            }

            // ✅ VALIDATION: Chỉ cho phép hoàn thành khi phiếu đang ở trạng thái IN_PROGRESS

            if (maintenanceTicket.StatusCode != "IN_PROGRESS")

                throw new ArgumentException($"Không thể hoàn thành phiếu ở trạng thái {maintenanceTicket.StatusCode}. Chỉ có thể hoàn thành khi phiếu ở trạng thái IN_PROGRESS.");



            // ✅ VALIDATION: Yêu cầu có ít nhất 1 service task
            var serviceTasks = await _serviceTaskRepository.GetByMaintenanceTicketIdAsync(id);
            if (!serviceTasks.Any())
                throw new ArgumentException("Không thể hoàn thành phiếu chưa có công việc nào");

            // ✅ VALIDATION: Kiểm tra tất cả ServiceTasks phải hoàn thành (DONE hoặc COMPLETED)
            {
                var incompleteTasks = serviceTasks
                    .Where(st => st.StatusCode != "DONE" && st.StatusCode != "COMPLETED")
                    .ToList();
                

                if (incompleteTasks.Any())

                {

                    var incompleteTaskNames = incompleteTasks

                        .Select(t => t.TaskName ?? $"Task ID: {t.Id}")

                        .Take(5)

                        .ToList();
                    


                    var message = $"Không thể hoàn thành phiếu. Còn {incompleteTasks.Count} công việc chưa hoàn thành: {string.Join(", ", incompleteTaskNames)}";

                    if (incompleteTasks.Count > 5)

                        message += $" và {incompleteTasks.Count - 5} công việc khác.";
                    


                    throw new ArgumentException(message);

                }

            }

            // ✅ VALIDATION: Kiểm tra TicketComponents (nếu có Service Package hoặc đã thêm components)
            // Nếu có Service Package hoặc đã có components trong phiếu, phải có ít nhất 1 component được ghi nhận
            if (maintenanceTicket.ServicePackageId.HasValue)
            {
                // ✅ Load TicketComponents từ database để đảm bảo có dữ liệu mới nhất
                var ticketComponents = await _context.TicketComponents
                    .Include(tc => tc.Component)
                    .Where(tc => tc.MaintenanceTicketId == id)
                    .ToListAsync();
                
                if (!ticketComponents.Any())
                {
                    throw new ArgumentException("Không thể hoàn thành phiếu. Phiếu có gói dịch vụ nhưng chưa có linh kiện nào được thêm vào.");
                }
                
                // Kiểm tra tất cả components phải có số lượng thực tế sử dụng > 0
                // Sử dụng ActualQuantity thay vì QuantityUsed (theo model TicketComponent)
                var invalidComponents = ticketComponents
                    .Where(tc => !tc.ActualQuantity.HasValue || tc.ActualQuantity.Value <= 0)
                    .ToList();
                
                if (invalidComponents.Any())
                {
                    var componentNames = invalidComponents
                        .Select(tc => tc.Component?.Name ?? $"Component ID: {tc.ComponentId}")
                        .Take(5)
                        .ToList();
                    
                    var message = $"Không thể hoàn thành phiếu. Có {invalidComponents.Count} linh kiện chưa được ghi nhận số lượng thực tế sử dụng: {string.Join(", ", componentNames)}";
                    if (invalidComponents.Count > 5)
                        message += $" và {invalidComponents.Count - 5} linh kiện khác.";
                    
                    throw new ArgumentException(message);
                }
            }
            else
            {
                // ✅ Nếu không có ServicePackage, kiểm tra xem có TicketComponents được thêm thủ công không
                var ticketComponents = await _context.TicketComponents
                    .Include(tc => tc.Component)
                    .Where(tc => tc.MaintenanceTicketId == id)
                    .ToListAsync();
                
                if (ticketComponents.Any())
                {
                    // Nếu có components được thêm thủ công, phải có số lượng thực tế > 0
                    var invalidComponents = ticketComponents
                        .Where(tc => !tc.ActualQuantity.HasValue || tc.ActualQuantity.Value <= 0)
                        .ToList();
                    
                    if (invalidComponents.Any())
                    {
                        var componentNames = invalidComponents
                            .Select(tc => tc.Component?.Name ?? $"Component ID: {tc.ComponentId}")
                            .Take(5)
                            .ToList();
                        
                        var message = $"Không thể hoàn thành phiếu. Có {invalidComponents.Count} linh kiện chưa được ghi nhận số lượng thực tế sử dụng: {string.Join(", ", componentNames)}";
                        if (invalidComponents.Count > 5)
                            message += $" và {invalidComponents.Count - 5} linh kiện khác.";
                        
                        throw new ArgumentException(message);
                    }
                }
            }

            // ✅ Hoàn lại phần chênh lệch vào kho nếu ActualQuantity < Quantity
            // Load tất cả TicketComponents để kiểm tra và hoàn lại kho
            var allTicketComponents = await _context.TicketComponents
                .Include(tc => tc.Component)
                .Where(tc => tc.MaintenanceTicketId == id)
                .ToListAsync();
            
            foreach (var ticketComponent in allTicketComponents)
            {
                // Nếu số lượng thực tế < số lượng đã lấy, hoàn lại phần chênh lệch
                if (ticketComponent.ActualQuantity.HasValue && 
                    ticketComponent.Component != null && 
                    ticketComponent.Component.QuantityStock.HasValue &&
                    ticketComponent.ActualQuantity.Value < ticketComponent.Quantity)
                {
                    var quantityToReturn = ticketComponent.Quantity - (int)Math.Ceiling(ticketComponent.ActualQuantity.Value);
                    ticketComponent.Component.QuantityStock += quantityToReturn;
                    _context.Components.Update(ticketComponent.Component);
                }
            }
            
            // Lưu thay đổi số lượng tồn kho
            if (allTicketComponents.Any(tc => tc.ActualQuantity.HasValue && tc.ActualQuantity.Value < tc.Quantity))
            {
                await _context.SaveChangesAsync();
            }

            // Lưu trạng thái cũ để ghi log

            var oldStatus = maintenanceTicket.StatusCode;



            maintenanceTicket.StatusCode = "COMPLETED";

            maintenanceTicket.EndTime = DateTime.UtcNow;

            var updatedTicket = await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);



            // ✅ Tạo history log để ghi nhận việc hoàn thành bảo dưỡng

            var userName = maintenanceTicket.Technician != null 

                ? ($"{maintenanceTicket.Technician.FirstName} {maintenanceTicket.Technician.LastName}").Trim() 

                : (maintenanceTicket.Consulter != null 

                    ? ($"{maintenanceTicket.Consulter.FirstName} {maintenanceTicket.Consulter.LastName}").Trim() 

                    : "Unknown");
            


            await CreateHistoryLogAsync(

                userId: maintenanceTicket.TechnicianId ?? maintenanceTicket.ConsulterId,

                action: "COMPLETE_MAINTENANCE",

                maintenanceTicketId: id,

                oldData: $"Status: {oldStatus}",

                newData: $"Phiếu hoàn thành bởi {userName}"

            );
            try
            {
                var receiptRequest = new TotalReceiptRequestDto
                {
                    MaintenanceTicketId = maintenanceTicket.Id,
                    CarId = maintenanceTicket.CarId,
                    BranchId = maintenanceTicket.BranchId,
                    CreatedAt = DateTime.UtcNow
                };
                await _totalReceiptService.CreateAsync(receiptRequest);
            }
            catch (Exception)
            {
                // Ignore failures when auto-creating receipt; optionally log if needed
            }

            // ✅ Load lại với ServicePackage để map đầy đủ
            var fullUpdatedTicket = await _maintenanceTicketRepository.GetByIdAsync(updatedTicket.Id);
            return MapToResponseWithServicePackage(fullUpdatedTicket);

        }



        public async Task<ResponseDto> CancelMaintenanceTicketAsync(long id)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");



            // ✅ VALIDATION: Không cho phép hủy phiếu đã hoàn thành hoặc đã hủy

            if (maintenanceTicket.StatusCode == "COMPLETED")

                throw new ArgumentException("Không thể hủy phiếu đã hoàn thành.");



            if (maintenanceTicket.StatusCode == "CANCELLED")

                throw new ArgumentException("Phiếu này đã được hủy trước đó.");

            // ✅ VALIDATION: Không cho phép hủy khi đang IN_PROGRESS (có thể mở tùy chọn quyền)
            if (maintenanceTicket.StatusCode == "IN_PROGRESS")
                throw new ArgumentException("Không thể hủy phiếu khi đang thực hiện (IN_PROGRESS).");



            // Lưu trạng thái cũ để ghi log

            var oldStatus = maintenanceTicket.StatusCode;

            // ✅ Hoàn lại tất cả linh kiện vào kho khi hủy phiếu
            var ticketComponents = await _context.TicketComponents
                .Include(tc => tc.Component)
                .Where(tc => tc.MaintenanceTicketId == id)
                .ToListAsync();
            
            foreach (var ticketComponent in ticketComponents)
            {
                // Hoàn lại số lượng đã lấy từ kho (Quantity)
                if (ticketComponent.Component != null && ticketComponent.Component.QuantityStock.HasValue)
                {
                    var quantityToReturn = ticketComponent.Quantity;
                    ticketComponent.Component.QuantityStock += quantityToReturn;
                    _context.Components.Update(ticketComponent.Component);
                }
            }
            
            // Lưu thay đổi số lượng tồn kho
            if (ticketComponents.Any())
            {
                await _context.SaveChangesAsync();
            }

            maintenanceTicket.StatusCode = "CANCELLED";

            var updatedTicket = await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);



            // ✅ Tạo history log để ghi nhận việc hủy phiếu

            var userName = maintenanceTicket.Consulter != null 

                ? ($"{maintenanceTicket.Consulter.FirstName} {maintenanceTicket.Consulter.LastName}").Trim() 

                : "Unknown";
            


            await CreateHistoryLogAsync(

                userId: maintenanceTicket.ConsulterId,

                action: "CANCEL_MAINTENANCE",

                maintenanceTicketId: id,

                oldData: $"Status: {oldStatus}",

                newData: $"Phiếu bị hủy bởi {userName}"

            );



            // ✅ Load lại với ServicePackage để map đầy đủ
            var fullUpdatedTicket = await _maintenanceTicketRepository.GetByIdAsync(updatedTicket.Id);
            return MapToResponseWithServicePackage(fullUpdatedTicket);

        }

        public async Task<List<UserMaintenanceHistoryDto>> GetMaintenanceHistoryByUserIdAsync(long userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("Invalid userId");
            }

            // ✅ Load tất cả tickets của user từ TẤT CẢ chi nhánh, không filter theo branchId
            var tickets = await _maintenanceTicketRepository.GetByUserIdAsync(userId);
            var result = new List<UserMaintenanceHistoryDto>();

            foreach (var ticket in tickets)
            {
                // ✅ Load receipt mới nhất (có thể có nhiều receipts)
                var receipt = ticket.TotalReceipts?
                    .OrderByDescending(r => r.CreatedAt ?? DateTime.MinValue)
                    .FirstOrDefault();

                // ✅ Chỉ hiển thị các phiếu đã thanh toán (hóa đơn có status = PAID)
                if (!string.Equals(receipt?.StatusCode, "PAID", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(new UserMaintenanceHistoryDto
                {
                    TicketId = ticket.Id,
                    TicketCode = ticket.Code,
                    StatusCode = ticket.StatusCode,
                    StatusName = GetStatusDisplayName(ticket.StatusCode),
                    CreatedDate = ticket.CreatedAt,
                    StartTime = ticket.StartTime,
                    EndTime = ticket.EndTime,
                    CarName = ticket.SnapshotCarName ?? ticket.Car?.CarName,
                    LicensePlate = ticket.SnapshotLicensePlate ?? ticket.Car?.LicensePlate,
                    BranchName = ticket.SnapshotBranchName ?? ticket.Branch?.Name,
                    ServiceCategoryId = ticket.ServiceCategoryId,
                    ServiceCategoryName = ticket.ServiceCategory?.Name,
                    ReceiptAmount = receipt?.FinalAmount ?? receipt?.Amount,
                    ReceiptId = receipt?.Id,
                    ReceiptCode = receipt != null ? $"REC-{receipt.Id}" : null,
                    ReceiptStatus = receipt?.StatusCode
                });
            }

            return result;
        }



        /// <summary>

        /// Tạo mã phiếu bảo dưỡng 7 ký tự ngẫu nhiên (số + chữ cái), đảm bảo không trùng

        /// </summary>

        private async Task<string> GenerateUniqueCodeAsync()

        {

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            const int codeLength = 7;

            var random = new Random();

            string code;


            
            // Tạo lại code cho đến khi không trùng

            do

            {

                code = new string(Enumerable.Repeat(chars, codeLength)

                    .Select(s => s[random.Next(s.Length)]).ToArray());

            } while (await _maintenanceTicketRepository.CodeExistsAsync(code));


            
            return code;

        }



        /// <summary>

        /// Helper method để tạo history log

        /// </summary>

        private async Task CreateHistoryLogAsync(long? userId, string action, long? maintenanceTicketId = null, string? oldData = null, string? newData = null)

        {

            var historyLog = new HistoryLog

            {

                UserId = userId,

                MaintenanceTicketId = maintenanceTicketId,

                Action = action,

                OldData = oldData,

                NewData = newData,

                CreatedAt = DateTime.UtcNow

            };

            await _historyLogRepository.CreateAsync(historyLog);

        }



        /// <summary>

        /// Lấy lịch sử hoạt động của Maintenance Ticket

        /// </summary>

        public async Task<List<BE.DTOs.HistoryLog.ResponseDto>> GetHistoryLogsAsync(long id)

        {

            var historyLogs = await _historyLogRepository.GetByMaintenanceTicketIdAsync(id);

            return historyLogs.Select(h => new BE.DTOs.HistoryLog.ResponseDto

            {

                Id = h.Id,

                UserId = h.UserId,

                UserName = h.User != null ? ($"{h.User.FirstName} {h.User.LastName}").Trim() : null,

                Action = h.Action,

                NewData = h.NewData,

                CreatedAt = h.CreatedAt

            }).ToList();

        }



        /// <summary>

        /// Áp dụng Service Package vào Maintenance Ticket - tự động thêm các Components từ package

        /// </summary>

        public async Task<ResponseDto> ApplyServicePackageAsync(long id, long servicePackageId)

        {

            // Validate MaintenanceTicket exists

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            if (maintenanceTicket == null)

                throw new ArgumentException("Maintenance ticket not found");



            // ✅ VALIDATION: Chỉ cho phép áp dụng Service Package khi phiếu ở trạng thái PENDING, ASSIGNED hoặc IN_PROGRESS

            if (maintenanceTicket.StatusCode != "PENDING" && maintenanceTicket.StatusCode != "ASSIGNED" && maintenanceTicket.StatusCode != "IN_PROGRESS")

                throw new ArgumentException($"Không thể áp dụng gói dịch vụ cho phiếu có trạng thái {maintenanceTicket.StatusCode}. Chỉ có thể áp dụng khi phiếu ở trạng thái PENDING, ASSIGNED hoặc IN_PROGRESS.");



            // Validate ServicePackage exists

            var servicePackage = await _servicePackageService.GetByIdAsync(servicePackageId);

            if (servicePackage == null)

                throw new ArgumentException("Service package not found");



            // Validate branch match

            if (servicePackage.BranchId != maintenanceTicket.BranchId)

                throw new ArgumentException("Service package không thuộc chi nhánh của phiếu bảo dưỡng");



            // ✅ Lấy danh sách phụ tùng hiện có trong phiếu (KHÔNG XÓA, CHỈ THÊM)
            var existingComponents = await _ticketComponentService.GetByMaintenanceTicketIdAsync(id);

            // ✅ Tạo HashSet để kiểm tra nhanh phụ tùng đã tồn tại
            var existingComponentIds = existingComponents
                .Select(c => c.ComponentId)
                .ToHashSet();

            // ✅ Lưu số lượng phụ tùng ban đầu để đảm bảo không bị mất
            var initialComponentCount = existingComponents.Count();



            // Get components from ServicePackage

            if (servicePackage.Components == null || !servicePackage.Components.Any())

                throw new ArgumentException("Service package không có phụ tùng nào");




            // ✅ CHỈ THÊM phụ tùng từ gói vào phiếu, KHÔNG XÓA phụ tùng hiện có
            var addedComponents = new List<string>();

            var skippedComponents = new List<string>();

            // ✅ Kiểm tra và báo lỗi nếu có linh kiện "Không hoạt động" trong gói dịch vụ
            var inactiveComponents = servicePackage.Components
                .Where(c => c.StatusCode != null && c.StatusCode != "ACTIVE")
                .Select(c => c.Name ?? $"Component ID: {c.Id}")
                .ToList();

            if (inactiveComponents.Any())
            {
                var inactiveList = string.Join(", ", inactiveComponents);
                throw new ArgumentException($"Không thể áp dụng gói dịch vụ vì có {inactiveComponents.Count} linh kiện không hoạt động: {inactiveList}. Vui lòng kiểm tra lại trạng thái các linh kiện trong gói dịch vụ.");
            }

            
            foreach (var packageComponent in servicePackage.Components)

            {
                // ✅ Bỏ qua nếu phụ tùng đã tồn tại trong phiếu (giữ nguyên phụ tùng hiện có)
                if (existingComponentIds.Contains(packageComponent.Id))

                {

                    skippedComponents.Add(packageComponent.Name ?? $"Component ID: {packageComponent.Id}");

                    continue;

                }



                try

                {

                    // Add component to ticket (quantity = 1, use component's unit price)

                    var ticketComponentDto = new BE.DTOs.TicketComponent.RequestDto

                    {

                        MaintenanceTicketId = id,

                        ComponentId = packageComponent.Id,

                        Quantity = 1, // Default quantity = 1

                        UnitPrice = packageComponent.UnitPrice,
                        
                        ServicePackageId = servicePackageId // ✅ Đánh dấu phụ tùng từ gói dịch vụ

                    };



                    await _ticketComponentService.CreateAsync(ticketComponentDto);

                    addedComponents.Add(packageComponent.Name ?? $"Component ID: {packageComponent.Id}");

                }

                catch (Exception ex)

                {

                    // Log error but continue with other components
                }

            }



            // Create history log

            var consulterName = maintenanceTicket.Consulter != null 

                ? ($"{maintenanceTicket.Consulter.FirstName} {maintenanceTicket.Consulter.LastName}").Trim() 

                : "Unknown";
            


            // ✅ Đảm bảo không có phụ tùng nào bị xóa
            var finalComponents = await _ticketComponentService.GetByMaintenanceTicketIdAsync(id);
            var finalComponentCount = finalComponents.Count();
            
            if (finalComponentCount < initialComponentCount)
            {
                throw new InvalidOperationException($"Lỗi: Số lượng phụ tùng giảm từ {initialComponentCount} xuống {finalComponentCount}. Phụ tùng hiện có không được phép bị xóa khi áp dụng gói dịch vụ.");
            }

            var logMessage = $"Áp dụng gói dịch vụ '{servicePackage.Name}' bởi {consulterName}. ";

            if (addedComponents.Any())

                logMessage += $"Đã thêm {addedComponents.Count} phụ tùng: {string.Join(", ", addedComponents)}. ";

            if (skippedComponents.Any())

                logMessage += $"Đã bỏ qua {skippedComponents.Count} phụ tùng (đã tồn tại, giữ nguyên): {string.Join(", ", skippedComponents)}. ";
            
            logMessage += $" Tổng số phụ tùng: {initialComponentCount} → {finalComponentCount}.";



            // ❌ ĐÃ LOẠI BỎ: Logic tự động tạo ServiceTasks từ ServiceCategories

            // Lý do: ServiceCategory chỉ dùng cho ScheduleService (đặt lịch), không phải để tạo công việc thực tế

            // ServiceTasks nên được tạo thủ công bởi người dùng



            await CreateHistoryLogAsync(

                userId: maintenanceTicket.ConsulterId,

                action: "APPLY_SERVICE_PACKAGE",

                maintenanceTicketId: id,

                oldData: null,

                newData: logMessage

            );



            // ✅ Lưu thông tin Service Package vào MaintenanceTicket
            maintenanceTicket.ServicePackageId = servicePackageId;
            maintenanceTicket.ServicePackagePrice = servicePackage.Price;
            await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);

            // ✅ Cập nhật TotalEstimatedCost = ComponentTotal + LaborCostTotal

            await UpdateMaintenanceTicketTotalCost(id);



            // Return updated ticket

            var updatedTicket = await _maintenanceTicketRepository.GetByIdAsync(id);

            return MapToResponseWithServicePackage(updatedTicket);

        }

        /// <summary>
        /// Xóa gói dịch vụ đã áp dụng khỏi Maintenance Ticket - chỉ xóa thông tin gói dịch vụ, giữ lại các phụ tùng
        /// </summary>
        public async Task<ResponseDto> RemoveServicePackageAsync(long id)
        {
            // Validate MaintenanceTicket exists
            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(id);
            if (maintenanceTicket == null)
                throw new ArgumentException("Maintenance ticket not found");

            // ✅ VALIDATION: Chỉ cho phép xóa Service Package khi phiếu ở trạng thái PENDING, ASSIGNED hoặc IN_PROGRESS
            if (maintenanceTicket.StatusCode != "PENDING" && maintenanceTicket.StatusCode != "ASSIGNED" && maintenanceTicket.StatusCode != "IN_PROGRESS")
                throw new ArgumentException($"Không thể xóa gói dịch vụ cho phiếu có trạng thái {maintenanceTicket.StatusCode}. Chỉ có thể xóa khi phiếu ở trạng thái PENDING, ASSIGNED hoặc IN_PROGRESS.");

            // Check if service package is applied
            if (!maintenanceTicket.ServicePackageId.HasValue)
                throw new ArgumentException("Phiếu bảo dưỡng không có gói dịch vụ nào được áp dụng");

            var servicePackageId = maintenanceTicket.ServicePackageId.Value;
            var servicePackageName = maintenanceTicket.ServicePackage?.Name ?? $"Gói dịch vụ ID: {servicePackageId}";

            // ✅ Xóa ServicePackageId khỏi các TicketComponent (không xóa components, chỉ xóa đánh dấu "Từ gói dịch vụ")
            var componentsFromPackage = await _context.TicketComponents
                .Where(tc => tc.MaintenanceTicketId == id && tc.ServicePackageId == servicePackageId)
                .ToListAsync();

            var componentNames = new List<string>();
            foreach (var component in componentsFromPackage)
            {
                component.ServicePackageId = null; // Xóa đánh dấu "Từ gói dịch vụ"
                _context.TicketComponents.Update(component);
                
                // Lấy tên component để log
                var componentEntity = await _context.Components.FindAsync(component.ComponentId);
                componentNames.Add(componentEntity?.Name ?? $"Component ID: {component.ComponentId}");
            }
            
            if (componentsFromPackage.Any())
            {
                await _context.SaveChangesAsync();
            }

            // Create history log
            var consulterName = maintenanceTicket.Consulter != null 
                ? ($"{maintenanceTicket.Consulter.FirstName} {maintenanceTicket.Consulter.LastName}").Trim() 
                : "Unknown";

            var logMessage = $"Xóa gói dịch vụ '{servicePackageName}' bởi {consulterName}. ";
            if (componentNames.Any())
                logMessage += $"Đã xóa đánh dấu 'Từ gói dịch vụ' khỏi {componentNames.Count} phụ tùng (phụ tùng vẫn được giữ lại): {string.Join(", ", componentNames)}.";

            await CreateHistoryLogAsync(
                userId: maintenanceTicket.ConsulterId,
                action: "REMOVE_SERVICE_PACKAGE",
                maintenanceTicketId: id,
                oldData: $"ServicePackageId: {servicePackageId}, ServicePackageName: {servicePackageName}",
                newData: logMessage
            );

            // ✅ Xóa thông tin Service Package khỏi MaintenanceTicket
            maintenanceTicket.ServicePackageId = null;
            maintenanceTicket.ServicePackagePrice = null;
            await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);

            // ✅ Cập nhật TotalEstimatedCost = ComponentTotal + LaborCostTotal
            await UpdateMaintenanceTicketTotalCost(id);

            // Return updated ticket
            var updatedTicket = await _maintenanceTicketRepository.GetByIdAsync(id);
            return MapToResponseWithServicePackage(updatedTicket);
        }

        /// <summary>
        /// ✅ Helper method để map ServicePackage info vào ResponseDto
        /// </summary>
        private ResponseDto MapToResponseWithServicePackage(MaintenanceTicket maintenanceTicket)
        {
            var response = _mapper.Map<ResponseDto>(maintenanceTicket);
            
            // ✅ Map thông tin Service Package
            response.ServicePackageId = maintenanceTicket.ServicePackageId;
            response.ServicePackagePrice = maintenanceTicket.ServicePackagePrice;
            response.ServicePackageName = maintenanceTicket.ServicePackage?.Name;
            
            // ✅ Map danh sách kỹ thuật viên
            if (maintenanceTicket.MaintenanceTicketTechnicians != null && maintenanceTicket.MaintenanceTicketTechnicians.Count > 0)
            {
                response.Technicians = maintenanceTicket.MaintenanceTicketTechnicians.Select(mtt => new TechnicianInfoDto
                {
                    TechnicianId = mtt.TechnicianId,
                    TechnicianName = mtt.Technician != null ? ($"{mtt.Technician.FirstName} {mtt.Technician.LastName}").Trim() : null,
                    RoleInTicket = mtt.RoleInTicket,
                    AssignedDate = mtt.AssignedDate
                }).ToList();
            }
            
            return response;
        }

        /// <summary>

        /// Cập nhật TotalEstimatedCost của MaintenanceTicket = ComponentTotal + LaborCostTotal

        /// </summary>

        private async Task UpdateMaintenanceTicketTotalCost(long maintenanceTicketId)

        {

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdWithCostDetailsAsync(maintenanceTicketId);


            
            if (maintenanceTicket == null)

                return;
            
            // Tính tổng phụ tùng

            var componentTotal = maintenanceTicket.TicketComponents

                .Sum(tc => (tc.ActualQuantity ?? tc.Quantity) * (tc.UnitPrice ?? 0));
            
            // Tính tổng phí nhân công

            var laborCostTotal = maintenanceTicket.ServiceTasks

                .Sum(st => st.LaborCost ?? 0);
            
            // Cập nhật TotalEstimatedCost

            maintenanceTicket.TotalEstimatedCost = componentTotal + laborCostTotal;

            await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);

        }

        private static string GetStatusDisplayName(string? statusCode)
        {
            if (string.IsNullOrWhiteSpace(statusCode))
            {
                return "Không xác định";
            }

            return statusCode.ToUpperInvariant() switch
            {
                "PENDING" => "Chờ xử lý",
                "ASSIGNED" => "Đã phân công",
                "IN_PROGRESS" => "Đang thực hiện",
                "WAITING_PAYMENT" => "Chờ thanh toán",
                "PAID" => "Đã thanh toán",
                "COMPLETED" => "Hoàn thành",
                "CANCELLED" => "Đã hủy",
                _ => statusCode.ToUpperInvariant()
            };
        }

    }

}









