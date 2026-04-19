using AutoMapper;

using BE.DTOs.ServiceTask;

using BE.interfaces;

using BE.repository.IRepository;

using BE.models;



namespace BE.services

{

    public class ServiceTaskService : IServiceTaskService

    {

        private readonly IServiceTaskRepository _serviceTaskRepository;
        private readonly IMaintenanceTicketRepository _maintenanceTicketRepository;
        private readonly IServiceCategoryRepository _serviceCategoryRepository;
        private readonly IHistoryLogRepository _historyLogRepository;
        private readonly CarMaintenanceDbContext _context;
        private readonly IMapper _mapper;



        public ServiceTaskService(
            IServiceTaskRepository serviceTaskRepository, 
            IMaintenanceTicketRepository maintenanceTicketRepository,
            IServiceCategoryRepository serviceCategoryRepository,
            IHistoryLogRepository historyLogRepository,
            CarMaintenanceDbContext context,
            IMapper mapper)

        {

            _serviceTaskRepository = serviceTaskRepository;
            _maintenanceTicketRepository = maintenanceTicketRepository;
            _serviceCategoryRepository = serviceCategoryRepository;
            _historyLogRepository = historyLogRepository;
            _context = context;
            _mapper = mapper;

        }



        public async Task<ServiceTaskResponseDto> CreateServiceTaskAsync(ServiceTaskRequestDto request, long? userId = null)
        {
            // ✅ VALIDATION: Kiểm tra trạng thái MaintenanceTicket - không cho thêm công việc nếu phiếu đã hoàn thành
            var ticket = await _maintenanceTicketRepository.GetByIdAsync(request.MaintenanceTicketId);
            if (ticket?.StatusCode == "COMPLETED" || ticket?.StatusCode == "CANCELLED")
            {
                throw new InvalidOperationException("Không thể thêm công việc sau khi phiếu đã hoàn thành hoặc đã hủy");
            }
            
            // ✅ VALIDATION: Kiểm tra kỹ thuật viên có được gán vào phiếu không (nếu có TechnicianId)
            if (request.TechnicianId.HasValue)
            {
                var isAssigned = ticket?.TechnicianId == request.TechnicianId.Value;
                if (!isAssigned && ticket?.MaintenanceTicketTechnicians != null)
                {
                    isAssigned = ticket.MaintenanceTicketTechnicians.Any(t => t.TechnicianId == request.TechnicianId.Value);
                }
                
                if (!isAssigned)
                {
                    throw new InvalidOperationException(
                        "Kỹ thuật viên phải được gán vào phiếu bảo dưỡng trước khi được gán vào công việc");
                }
            }
            
            var serviceTask = _mapper.Map<ServiceTask>(request);           
            // Tính LaborCost nếu có StandardLaborTime và Branch.LaborRate

            if (serviceTask.StandardLaborTime.HasValue && serviceTask.StandardLaborTime.Value > 0)
            {
                // Lấy MaintenanceTicket để lấy Branch
                var maintenanceTicket = await _maintenanceTicketRepository.GetByIdWithBranchAsync(request.MaintenanceTicketId);
                if (maintenanceTicket?.Branch?.LaborRate.HasValue != true || maintenanceTicket.Branch!.LaborRate!.Value <= 0)
                    throw new ArgumentException("Branch.LaborRate phải được cấu hình khi sử dụng StandardLaborTime");
               serviceTask.LaborCost = serviceTask.StandardLaborTime.Value * maintenanceTicket.Branch.LaborRate.Value;

            }           
            // Nếu có ServiceCategoryId nhưng chưa có StandardLaborTime, lấy từ ServiceCategory
            if (serviceTask.ServiceCategoryId.HasValue && !serviceTask.StandardLaborTime.HasValue)
            {
                var serviceCategory = await _serviceCategoryRepository.GetByIdAsync(serviceTask.ServiceCategoryId.Value);
                if (serviceCategory?.StandardLaborTime.HasValue == true)
                {
                    serviceTask.StandardLaborTime = serviceCategory.StandardLaborTime;
                    // Nếu chưa có ActualLaborTime, set bằng StandardLaborTime
                    if (!serviceTask.ActualLaborTime.HasValue)
                    {
                        serviceTask.ActualLaborTime = serviceCategory.StandardLaborTime;
                    }                   
                    // Tính LaborCost dựa trên StandardLaborTime
                    var maintenanceTicket = await _maintenanceTicketRepository.GetByIdWithBranchAsync(request.MaintenanceTicketId);
                    if (maintenanceTicket?.Branch?.LaborRate.HasValue != true || maintenanceTicket.Branch!.LaborRate!.Value <= 0)
                        throw new ArgumentException("Branch.LaborRate phải được cấu hình khi sử dụng StandardLaborTime");
                    serviceTask.LaborCost = serviceTask.StandardLaborTime.Value * maintenanceTicket.Branch.LaborRate.Value;
                }

            }

            

            var createdTask = await _serviceTaskRepository.CreateAsync(serviceTask);

            

            // Cập nhật TotalEstimatedCost của MaintenanceTicket

            await UpdateMaintenanceTicketTotalCost(serviceTask.MaintenanceTicketId ?? 0);

            // ✅ Tạo history log để ghi nhận việc thêm công việc
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                var userName = user != null 
                    ? ($"{user.FirstName} {user.LastName}").Trim() 
                    : "Unknown";
                
                var taskDetails = $"Thêm công việc '{createdTask.TaskName ?? "N/A"}'";
                if (createdTask.StandardLaborTime.HasValue)
                {
                    taskDetails += $" - Thời gian chuẩn: {createdTask.StandardLaborTime.Value} giờ";
                }
                if (createdTask.LaborCost.HasValue)
                {
                    taskDetails += $" - Phí nhân công: {createdTask.LaborCost.Value:N0} ₫";
                }
                
                await CreateHistoryLogAsync(
                    userId: userId,
                    action: "ADD_SERVICE_TASK",
                    maintenanceTicketId: serviceTask.MaintenanceTicketId,
                    newData: $"{taskDetails} bởi {userName}"
                );
            }

            

            return await GetServiceTaskByIdAsync(createdTask.Id);

        }



        public async Task<ServiceTaskResponseDto> UpdateServiceTaskAsync(ServiceTaskUpdateDto request)

        {

            var existingTask = await _serviceTaskRepository.GetByIdAsync(request.Id);

            if (existingTask == null)

                throw new ArgumentException("Service task not found");

            // ✅ VALIDATION: Kiểm tra trạng thái MaintenanceTicket - không cho sửa công việc nếu phiếu đã hoàn thành
            var ticket = await _maintenanceTicketRepository.GetByIdAsync(existingTask.MaintenanceTicketId ?? 0);
            if (ticket?.StatusCode == "COMPLETED" || ticket?.StatusCode == "CANCELLED")
            {
                throw new InvalidOperationException("Không thể sửa công việc sau khi phiếu đã hoàn thành hoặc đã hủy");
            }

            // ✅ VALIDATION: Không cho phép sửa công việc đã hoàn thành
            if (existingTask.StatusCode == "DONE" || existingTask.StatusCode == "COMPLETED")
            {
                throw new ArgumentException("Không thể sửa công việc đã hoàn thành");
            }

            // ✅ VALIDATION: Kiểm tra kỹ thuật viên có được gán vào phiếu không (nếu có TechnicianId mới)
            if (request.TechnicianId.HasValue && request.TechnicianId.Value != existingTask.TechnicianId)
            {
                var isAssigned = ticket?.TechnicianId == request.TechnicianId.Value;
                if (!isAssigned && ticket?.MaintenanceTicketTechnicians != null)
                {
                    isAssigned = ticket.MaintenanceTicketTechnicians.Any(t => t.TechnicianId == request.TechnicianId.Value);
                }
                
                if (!isAssigned)
                {
                    throw new InvalidOperationException(
                        "Kỹ thuật viên phải được gán vào phiếu bảo dưỡng trước khi được gán vào công việc");
                }
            }

            // ✅ Capture old values BEFORE mapping (for history log)
            var oldTaskName = existingTask.TaskName;
            var oldStatus = existingTask.StatusCode;
            var oldStandardTime = existingTask.StandardLaborTime;
            var oldActualTime = existingTask.ActualLaborTime;
            var oldTechnicianId = existingTask.TechnicianId;

            _mapper.Map(request, existingTask);

            // Tính lại LaborCost dựa trên StandardLaborTime

            if (existingTask.StandardLaborTime.HasValue && existingTask.StandardLaborTime.Value > 0)

            {

                var maintenanceTicket = await _maintenanceTicketRepository.GetByIdWithBranchAsync(existingTask.MaintenanceTicketId ?? 0);
                if (maintenanceTicket?.Branch?.LaborRate.HasValue != true || maintenanceTicket.Branch!.LaborRate!.Value <= 0)
                    throw new ArgumentException("Branch.LaborRate phải được cấu hình khi sử dụng StandardLaborTime");
                existingTask.LaborCost = existingTask.StandardLaborTime.Value * maintenanceTicket.Branch.LaborRate.Value;
            }

            else

            {

                // Nếu không có StandardLaborTime, set LaborCost = null

                existingTask.LaborCost = null;

            }

            // ✅ CẢNH BÁO: Khi thời gian thực tế vượt quá thời gian chuẩn nhiều (> 1.5 lần)
            if (existingTask.StandardLaborTime.HasValue && existingTask.ActualLaborTime.HasValue && 
                existingTask.StandardLaborTime.Value > 0)
            {
                var ratio = existingTask.ActualLaborTime.Value / existingTask.StandardLaborTime.Value;
                if (ratio > 1.5m)
                {
                    // Thêm cảnh báo vào Note nếu chưa có
                    var warningMessage = $"[CẢNH BÁO] Thời gian thực tế ({existingTask.ActualLaborTime.Value:F2}h) vượt quá thời gian chuẩn ({existingTask.StandardLaborTime.Value:F2}h) {(ratio * 100):F0}%. ";
                    if (string.IsNullOrWhiteSpace(existingTask.Note))
                    {
                        existingTask.Note = warningMessage;
                    }
                    else if (!existingTask.Note.Contains("[CẢNH BÁO]"))
                    {
                        existingTask.Note = warningMessage + "\n" + existingTask.Note;
                    }
                }
            }

            

            // ✅ Ghi log chi tiết khi cập nhật công việc
            var updatedTask = await _serviceTaskRepository.UpdateAsync(existingTask);

            // ✅ Tạo history log chi tiết khi cập nhật
            var changes = new List<string>();
            if (oldTaskName != request.TaskName)
            {
                changes.Add($"Tên: '{oldTaskName}' → '{request.TaskName}'");
            }
            if (oldStatus != request.StatusCode && !string.IsNullOrEmpty(request.StatusCode))
            {
                changes.Add($"Trạng thái: {oldStatus} → {request.StatusCode}");
            }
            if (oldStandardTime != request.StandardLaborTime)
            {
                changes.Add($"Thời gian chuẩn: {oldStandardTime} → {request.StandardLaborTime}");
            }
            if (oldActualTime != request.ActualLaborTime)
            {
                changes.Add($"Thời gian thực tế: {oldActualTime} → {request.ActualLaborTime}");
            }
            if (oldTechnicianId != request.TechnicianId)
            {
                changes.Add($"Kỹ thuật viên: {oldTechnicianId} → {request.TechnicianId}");
            }
            
            if (changes.Any())
            {
                await CreateHistoryLogAsync(
                    userId: null, // Có thể thêm userId parameter nếu cần
                    action: "UPDATE_SERVICE_TASK",
                    maintenanceTicketId: existingTask.MaintenanceTicketId,
                    oldData: $"Công việc '{oldTaskName}': {string.Join("; ", changes)}",
                    newData: $"Đã cập nhật công việc '{request.TaskName}'"
                );
            }

            // Cập nhật TotalEstimatedCost của MaintenanceTicket
            await UpdateMaintenanceTicketTotalCost(existingTask.MaintenanceTicketId ?? 0);         
            return await GetServiceTaskByIdAsync(updatedTask.Id);

        }



        public async Task<ServiceTaskResponseDto> GetServiceTaskByIdAsync(long id)
        {
            var serviceTask = await _serviceTaskRepository.GetByIdWithDetailsAsync(id);
            if (serviceTask == null)
                throw new ArgumentException("Service task not found");
            return MapToResponseDTO(serviceTask);

        }



        public async Task<List<ServiceTaskListResponseDto>> GetAllServiceTasksAsync(int page = 1, int pageSize = 10)
        {
            var serviceTasks = await _serviceTaskRepository.GetAllWithDetailsAsync(page, pageSize);
            return serviceTasks.Select(MapToListResponseDTO).ToList();
        }
        public async Task<List<ServiceTaskListResponseDto>> GetServiceTasksByMaintenanceTicketIdAsync(long maintenanceTicketId)
        {

            var serviceTasks = await _serviceTaskRepository.GetByMaintenanceTicketIdAsync(maintenanceTicketId);
            return serviceTasks.Select(MapToListResponseDTO).ToList();

        }



        public async Task<List<ServiceTaskListResponseDto>> GetServiceTasksByStatusAsync(string statusCode)

        {

            var serviceTasks = await _serviceTaskRepository.GetByStatusAsync(statusCode);

            return serviceTasks.Select(MapToListResponseDTO).ToList();

        }



        public async Task<List<ServiceTaskListResponseDto>> GetServiceTasksByTechnicianIdAsync(long technicianId)

        {

            var serviceTasks = await _serviceTaskRepository.GetByTechnicianIdAsync(technicianId);

            return serviceTasks.Select(MapToListResponseDTO).ToList();

        }



        public async Task<bool> DeleteServiceTaskAsync(long id)

        {

            var serviceTask = await _serviceTaskRepository.GetByIdAsync(id);

            if (serviceTask == null)

                return false;

            // ✅ VALIDATION: Kiểm tra trạng thái MaintenanceTicket - không cho xóa công việc nếu phiếu đã hoàn thành
            var ticket = await _maintenanceTicketRepository.GetByIdAsync(serviceTask.MaintenanceTicketId ?? 0);
            if (ticket?.StatusCode == "COMPLETED" || ticket?.StatusCode == "CANCELLED")
            {
                throw new InvalidOperationException("Không thể xóa công việc sau khi phiếu đã hoàn thành hoặc đã hủy");
            }

            // ✅ VALIDATION: Không cho phép xóa công việc đã hoàn thành
            if (serviceTask.StatusCode == "DONE" || serviceTask.StatusCode == "COMPLETED")
            {
                throw new ArgumentException("Không thể xóa công việc đã hoàn thành");
            }

            // ✅ Ghi log chi tiết trước khi xóa
            var deleteDetails = $"Xóa công việc '{serviceTask.TaskName}' - Trạng thái: {serviceTask.StatusCode}, Thời gian chuẩn: {serviceTask.StandardLaborTime}h, Thời gian thực tế: {serviceTask.ActualLaborTime}h";          

            var maintenanceTicketId = serviceTask.MaintenanceTicketId ?? 0;

            var result = await _serviceTaskRepository.DeleteAsync(id);
            
            // ✅ Tạo history log chi tiết khi xóa
            if (result)
            {
                await CreateHistoryLogAsync(
                    userId: null, // Có thể thêm userId parameter nếu cần
                    action: "DELETE_SERVICE_TASK",
                    maintenanceTicketId: maintenanceTicketId,
                    oldData: deleteDetails,
                    newData: "Đã xóa công việc khỏi phiếu bảo dưỡng"
                );
            }

            

            if (result)

            {

                // Cập nhật TotalEstimatedCost của MaintenanceTicket

                await UpdateMaintenanceTicketTotalCost(maintenanceTicketId);

            }

            

            return result;

        }



        public async Task<ServiceTaskResponseDto> UpdateStatusAsync(long id, string statusCode, long? userId = null, string? completionNote = null)

        {

            var serviceTask = await _serviceTaskRepository.GetByIdWithDetailsAsync(id);

            if (serviceTask == null)

                throw new ArgumentException("Service task not found");

            var oldStatus = serviceTask.StatusCode;

            // ✅ Tự động set StartTime khi chuyển sang IN_PROGRESS
            if (statusCode == "IN_PROGRESS" && oldStatus != "IN_PROGRESS" && !serviceTask.StartTime.HasValue)
            {
                serviceTask.StartTime = DateTime.UtcNow;
                
                // ✅ Nếu đây là ServiceTask đầu tiên chuyển sang IN_PROGRESS, set MaintenanceTicket.StartTime
                if (serviceTask.MaintenanceTicketId.HasValue)
                {
                    var maintenanceTicket = await _maintenanceTicketRepository.GetByIdAsync(serviceTask.MaintenanceTicketId.Value);
                    if (maintenanceTicket != null && !maintenanceTicket.StartTime.HasValue)
                    {
                        // Kiểm tra xem có ServiceTask nào khác đã IN_PROGRESS chưa
                        var allTasks = await _serviceTaskRepository.GetByMaintenanceTicketIdAsync(serviceTask.MaintenanceTicketId.Value);
                        var hasOtherInProgress = allTasks.Any(t => t.Id != id && t.StatusCode == "IN_PROGRESS");
                        
                        // Nếu không có ServiceTask nào khác đã IN_PROGRESS, đây là lần đầu tiên
                        if (!hasOtherInProgress)
                        {
                            maintenanceTicket.StartTime = DateTime.UtcNow;
                            await _maintenanceTicketRepository.UpdateAsync(maintenanceTicket);
                        }
                    }
                }
            }

            // ✅ Tự động set EndTime khi chuyển sang DONE hoặc COMPLETED
            if ((statusCode == "DONE" || statusCode == "COMPLETED") && oldStatus != statusCode && !serviceTask.EndTime.HasValue)
            {
                // ✅ VALIDATION: Không cho phép hoàn thành công việc nếu chưa có kỹ thuật viên được gán
                var hasTechnician = serviceTask.TechnicianId.HasValue;
                if (!hasTechnician)
                {
                    // Kiểm tra ServiceTaskTechnicians (đã được load từ GetByIdWithDetailsAsync)
                    hasTechnician = serviceTask.ServiceTaskTechnicians != null && 
                                   serviceTask.ServiceTaskTechnicians.Any();
                }
                
                if (!hasTechnician)
                {
                    throw new InvalidOperationException(
                        "Không thể hoàn thành công việc khi chưa có kỹ thuật viên được gán");
                }
                
                serviceTask.EndTime = DateTime.UtcNow;
                
                // ✅ VALIDATION: Thời gian kết thúc phải sau thời gian bắt đầu
                if (serviceTask.StartTime.HasValue && serviceTask.EndTime.HasValue)
                {
                    if (serviceTask.EndTime.Value < serviceTask.StartTime.Value)
                    {
                        throw new ArgumentException("Thời gian kết thúc phải sau thời gian bắt đầu");
                    }
                }
                
                // ✅ CHỈ tính ActualLaborTime tự động nếu CHƯA CÓ giá trị (làm gợi ý)
                // Lý do: Nhân viên có thể về nhà giữa chừng, thời gian thực tế làm việc 
                // không phải là EndTime - StartTime (có thể bỏ qua giờ nghỉ, giờ về nhà)
                if (serviceTask.StartTime.HasValue && serviceTask.EndTime.HasValue && !serviceTask.ActualLaborTime.HasValue)
                {
                    var timeSpan = serviceTask.EndTime.Value - serviceTask.StartTime.Value;
                    // Chuyển đổi từ TimeSpan sang giờ (decimal)
                    serviceTask.ActualLaborTime = (decimal)timeSpan.TotalHours;
                    
                    // Làm tròn đến 2 chữ số thập phân
                    serviceTask.ActualLaborTime = Math.Round(serviceTask.ActualLaborTime.Value, 2);
                    
                    // Lưu ý: Đây chỉ là gợi ý, nhân viên/quản lý có thể sửa lại thủ công
                }
            }

            // ✅ Set CompletionNote nếu có
            if (!string.IsNullOrWhiteSpace(completionNote))
            {
                serviceTask.CompletionNote = completionNote;
            }

            // ✅ CẢNH BÁO: Khi thời gian thực tế vượt quá thời gian chuẩn nhiều (> 1.5 lần)
            if (serviceTask.StandardLaborTime.HasValue && serviceTask.ActualLaborTime.HasValue && 
                serviceTask.StandardLaborTime.Value > 0)
            {
                var ratio = serviceTask.ActualLaborTime.Value / serviceTask.StandardLaborTime.Value;
                if (ratio > 1.5m)
                {
                    // Thêm cảnh báo vào CompletionNote nếu chưa có
                    var warningMessage = $"[CẢNH BÁO] Thời gian thực tế ({serviceTask.ActualLaborTime.Value:F2}h) vượt quá thời gian chuẩn ({serviceTask.StandardLaborTime.Value:F2}h) {(ratio * 100):F0}%. ";
                    if (string.IsNullOrWhiteSpace(serviceTask.CompletionNote))
                    {
                        serviceTask.CompletionNote = warningMessage;
                    }
                    else if (!serviceTask.CompletionNote.Contains("[CẢNH BÁO]"))
                    {
                        serviceTask.CompletionNote = warningMessage + "\n" + serviceTask.CompletionNote;
                    }
                    
                    // Ghi log cảnh báo
                    if (userId.HasValue)
                    {
                        await CreateHistoryLogAsync(
                            userId: userId,
                            action: "WARNING_LABOR_TIME_EXCEEDED",
                            maintenanceTicketId: serviceTask.MaintenanceTicketId,
                            newData: $"Cảnh báo: Công việc '{serviceTask.TaskName}' có thời gian thực tế vượt quá chuẩn {(ratio * 100):F0}%"
                        );
                    }
                }
            }

            serviceTask.StatusCode = statusCode;

            var updatedTask = await _serviceTaskRepository.UpdateAsync(serviceTask);

            // ✅ Tạo history log để ghi nhận việc cập nhật trạng thái công việc
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                var userName = user != null 
                    ? ($"{user.FirstName} {user.LastName}").Trim() 
                    : "Unknown";
                
                string action = "";
                string statusText = "";
                
                if (statusCode == "IN_PROGRESS" && oldStatus != "IN_PROGRESS")
                {
                    action = "START_SERVICE_TASK";
                    statusText = "Bắt đầu công việc";
                }
                else if ((statusCode == "DONE" || statusCode == "COMPLETED") && oldStatus != statusCode)
                {
                    action = "COMPLETE_SERVICE_TASK";
                    statusText = "Hoàn thành công việc";
                }
                else
                {
                    action = "UPDATE_SERVICE_TASK_STATUS";
                    statusText = "Cập nhật trạng thái công việc";
                }
                
                var taskDetails = $"{statusText} '{serviceTask.TaskName ?? "N/A"}'";
                if (oldStatus != statusCode)
                {
                    taskDetails += $" (Từ {oldStatus} → {statusCode})";
                }
                
                await CreateHistoryLogAsync(
                    userId: userId,
                    action: action,
                    maintenanceTicketId: serviceTask.MaintenanceTicketId,
                    oldData: $"Trạng thái cũ: {oldStatus}",
                    newData: $"{taskDetails} bởi {userName}"
                );
            }

            return await GetServiceTaskByIdAsync(updatedTask.Id);

        }



        public async Task<ServiceTaskResponseDto> UpdateLaborTimeAsync(long id, decimal actualLaborTime)

        {

            var serviceTask = await _serviceTaskRepository.GetByIdAsync(id);

            if (serviceTask == null)

                throw new ArgumentException("Service task not found");

            // ✅ CẢNH BÁO: Khi thời gian thực tế vượt quá thời gian chuẩn nhiều (> 1.5 lần)
            if (serviceTask.StandardLaborTime.HasValue && serviceTask.StandardLaborTime.Value > 0)
            {
                var ratio = actualLaborTime / serviceTask.StandardLaborTime.Value;
                if (ratio > 1.5m)
                {
                    // Ghi log cảnh báo
                    await CreateHistoryLogAsync(
                        userId: null,
                        action: "WARNING_LABOR_TIME_EXCEEDED",
                        maintenanceTicketId: serviceTask.MaintenanceTicketId,
                        newData: $"Cảnh báo: Công việc '{serviceTask.TaskName}' có thời gian thực tế ({actualLaborTime:F2}h) vượt quá chuẩn ({serviceTask.StandardLaborTime.Value:F2}h) {(ratio * 100):F0}%"
                    );
                }
            }

            serviceTask.ActualLaborTime = actualLaborTime;

            

            // Tính lại LaborCost dựa trên StandardLaborTime (không phải ActualLaborTime)

            var maintenanceTicket = await _maintenanceTicketRepository.GetByIdWithBranchAsync(serviceTask.MaintenanceTicketId ?? 0);

            

            if (serviceTask.StandardLaborTime.HasValue && serviceTask.StandardLaborTime.Value > 0)

            {

                if (maintenanceTicket?.Branch?.LaborRate.HasValue != true || maintenanceTicket.Branch!.LaborRate!.Value <= 0)

                    throw new ArgumentException("Branch.LaborRate phải được cấu hình khi sử dụng StandardLaborTime");

                serviceTask.LaborCost = serviceTask.StandardLaborTime.Value * maintenanceTicket.Branch.LaborRate.Value;

            }

            else

            {

                // Nếu không có StandardLaborTime, set LaborCost = null

                serviceTask.LaborCost = null;

            }

            

            var updatedTask = await _serviceTaskRepository.UpdateAsync(serviceTask);
                        // Cập nhật TotalEstimatedCost của MaintenanceTicket

            await UpdateMaintenanceTicketTotalCost(serviceTask.MaintenanceTicketId ?? 0);     
            return await GetServiceTaskByIdAsync(updatedTask.Id);

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


        private ServiceTaskResponseDto MapToResponseDTO(ServiceTask serviceTask)

        {

            return new ServiceTaskResponseDto

            {

                Id = serviceTask.Id,

                MaintenanceTicketId = serviceTask.MaintenanceTicketId ?? 0,

                TaskName = serviceTask.TaskName ?? string.Empty,

                Description = serviceTask.Description,

                StatusCode = serviceTask.StatusCode,

                Note = serviceTask.Note,



                // Labor cost fields

                ServiceCategoryId = serviceTask.ServiceCategoryId,

                StandardLaborTime = serviceTask.StandardLaborTime,

                ActualLaborTime = serviceTask.ActualLaborTime,

                LaborCost = serviceTask.LaborCost,

                // New fields
                TechnicianId = serviceTask.TechnicianId,
                StartTime = serviceTask.StartTime,
                EndTime = serviceTask.EndTime,
                DisplayOrder = serviceTask.DisplayOrder,
                CompletionNote = serviceTask.CompletionNote,

                // Navigation properties
                MaintenanceTicketCode = serviceTask.MaintenanceTicket?.Code,
                MaintenanceTicketDescription = serviceTask.MaintenanceTicket?.Description,

                CarName = serviceTask.MaintenanceTicket?.Car?.CarName,

                CustomerName = serviceTask.MaintenanceTicket?.Car?.User != null 

                    ? $"{serviceTask.MaintenanceTicket.Car.User.FirstName} {serviceTask.MaintenanceTicket.Car.User.LastName}".Trim()

                    : null,

                TechnicianName = serviceTask.Technician != null
                    ? $"{serviceTask.Technician.FirstName} {serviceTask.Technician.LastName}".Trim()
                    : (serviceTask.MaintenanceTicket?.Technician != null
                    ? $"{serviceTask.MaintenanceTicket.Technician.FirstName} {serviceTask.MaintenanceTicket.Technician.LastName}".Trim()
                        : null),

                BranchName = serviceTask.MaintenanceTicket?.Branch?.Name,

                ServiceCategoryName = serviceTask.ServiceCategory?.Name,
                
                // ✅ Map danh sách nhiều kỹ thuật viên
                Technicians = serviceTask.ServiceTaskTechnicians?.Select(stt => new ServiceTaskTechnicianDto
                {
                    TechnicianId = stt.TechnicianId,
                    TechnicianName = stt.Technician != null 
                        ? $"{stt.Technician.FirstName} {stt.Technician.LastName}".Trim() 
                        : null,
                    RoleInTask = stt.RoleInTask,
                    AssignedDate = stt.AssignedDate
                }).ToList()

            };

        }



        private ServiceTaskListResponseDto MapToListResponseDTO(ServiceTask serviceTask)

        {

            return new ServiceTaskListResponseDto
            {
                Id = serviceTask.Id,
                MaintenanceTicketId = serviceTask.MaintenanceTicketId ?? 0,
                TaskName = serviceTask.TaskName ?? string.Empty,
                Description = serviceTask.Description,
                StatusCode = serviceTask.StatusCode,
                Note = serviceTask.Note,
                // Labor cost fields

                ServiceCategoryId = serviceTask.ServiceCategoryId,

                StandardLaborTime = serviceTask.StandardLaborTime,

                ActualLaborTime = serviceTask.ActualLaborTime,

                LaborCost = serviceTask.LaborCost,

                // New fields
                TechnicianId = serviceTask.TechnicianId,
                StartTime = serviceTask.StartTime,
                EndTime = serviceTask.EndTime,
                DisplayOrder = serviceTask.DisplayOrder,
                CompletionNote = serviceTask.CompletionNote,

                // Basic info
                MaintenanceTicketCode = serviceTask.MaintenanceTicket?.Code,
                CarName = serviceTask.MaintenanceTicket?.Car?.CarName,

                CustomerName = serviceTask.MaintenanceTicket?.Car?.User != null 

                    ? $"{serviceTask.MaintenanceTicket.Car.User.FirstName} {serviceTask.MaintenanceTicket.Car.User.LastName}".Trim()

                    : null,

                TechnicianName = serviceTask.Technician != null
                    ? $"{serviceTask.Technician.FirstName} {serviceTask.Technician.LastName}".Trim()
                    : (serviceTask.MaintenanceTicket?.Technician != null
                    ? $"{serviceTask.MaintenanceTicket.Technician.FirstName} {serviceTask.MaintenanceTicket.Technician.LastName}".Trim()
                        : null),

                BranchName = serviceTask.MaintenanceTicket?.Branch?.Name,

                ServiceCategoryName = serviceTask.ServiceCategory?.Name,
                
                // ✅ Map danh sách nhiều kỹ thuật viên
                Technicians = serviceTask.ServiceTaskTechnicians?.Select(stt => new ServiceTaskTechnicianDto
                {
                    TechnicianId = stt.TechnicianId,
                    TechnicianName = stt.Technician != null 
                        ? $"{stt.Technician.FirstName} {stt.Technician.LastName}".Trim() 
                        : null,
                    RoleInTask = stt.RoleInTask,
                    AssignedDate = stt.AssignedDate
                }).ToList()

            };

        }

        /// <summary>
        /// Gán kỹ thuật viên cho ServiceTask
        /// </summary>
        public async Task<ServiceTaskResponseDto> AssignTechniciansAsync(long id, ServiceTaskAssignTechniciansDto request, long? userId = null)
        {
            var serviceTask = await _serviceTaskRepository.GetByIdWithDetailsAsync(id);
            if (serviceTask == null)
                throw new ArgumentException("Service task not found");

            // ✅ VALIDATION: Kiểm tra kỹ thuật viên có được gán vào phiếu không
            var ticket = await _maintenanceTicketRepository.GetByIdAsync(serviceTask.MaintenanceTicketId ?? 0);
            if (ticket == null)
                throw new ArgumentException("Maintenance ticket not found");

            if (request.TechnicianIds != null && request.TechnicianIds.Count > 0)
            {
                foreach (var technicianId in request.TechnicianIds)
                {
                    var isAssigned = ticket.TechnicianId == technicianId;
                    if (!isAssigned && ticket.MaintenanceTicketTechnicians != null)
                    {
                        isAssigned = ticket.MaintenanceTicketTechnicians.Any(t => t.TechnicianId == technicianId);
                    }
                    
                    if (!isAssigned)
                    {
                        throw new InvalidOperationException(
                            $"Kỹ thuật viên (ID: {technicianId}) phải được gán vào phiếu bảo dưỡng trước khi được gán vào công việc");
                    }
                }
            }
            
            // Nếu có PrimaryTechnicianId, cũng cần kiểm tra
            if (request.PrimaryTechnicianId.HasValue)
            {
                var primaryId = request.PrimaryTechnicianId.Value;
                var isAssigned = ticket.TechnicianId == primaryId;
                if (!isAssigned && ticket.MaintenanceTicketTechnicians != null)
                {
                    isAssigned = ticket.MaintenanceTicketTechnicians.Any(t => t.TechnicianId == primaryId);
                }
                
                if (!isAssigned)
                {
                    throw new InvalidOperationException(
                        $"Kỹ thuật viên chính (ID: {primaryId}) phải được gán vào phiếu bảo dưỡng trước khi được gán vào công việc");
                }
                
                // PrimaryTechnicianId phải nằm trong danh sách TechnicianIds
                if (request.TechnicianIds == null || !request.TechnicianIds.Contains(primaryId))
                {
                    throw new ArgumentException("Kỹ thuật viên chính phải nằm trong danh sách kỹ thuật viên được gán");
                }
            }

            // Xóa tất cả technicians hiện tại
            var existingTechnicians = _context.ServiceTaskTechnicians
                .Where(stt => stt.ServiceTaskId == id)
                .ToList();
            _context.ServiceTaskTechnicians.RemoveRange(existingTechnicians);

            // Thêm technicians mới
            if (request.TechnicianIds != null && request.TechnicianIds.Count > 0)
            {
                var newTechnicians = request.TechnicianIds.Select(techId => new ServiceTaskTechnician
                {
                    ServiceTaskId = id,
                    TechnicianId = techId,
                    AssignedDate = DateTime.UtcNow,
                    RoleInTask = (request.PrimaryTechnicianId.HasValue && techId == request.PrimaryTechnicianId.Value) 
                        ? "PRIMARY" 
                        : "ASSISTANT"
                }).ToList();

                _context.ServiceTaskTechnicians.AddRange(newTechnicians);
            }

            await _context.SaveChangesAsync();

            // ✅ Tạo history log
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                var userName = user != null 
                    ? ($"{user.FirstName} {user.LastName}").Trim() 
                    : "Unknown";
                
                var techNames = request.TechnicianIds?.Select(techId => 
                {
                    var tech = _context.Users.Find(techId);
                    return tech != null ? $"{tech.FirstName} {tech.LastName}".Trim() : $"#{techId}";
                }).ToList() ?? new List<string>();
                
                var techList = string.Join(", ", techNames);
                await CreateHistoryLogAsync(
                    userId: userId,
                    action: "ASSIGN_SERVICE_TASK_TECHNICIANS",
                    maintenanceTicketId: serviceTask.MaintenanceTicketId,
                    newData: $"Gán kỹ thuật viên cho công việc '{serviceTask.TaskName ?? "N/A"}': {techList} bởi {userName}"
                );
            }

            return await GetServiceTaskByIdAsync(id);
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

    }

}






      