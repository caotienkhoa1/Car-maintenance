namespace BE.DTOs.ServiceTask
{
    /// <summary>
    /// DTO cho response chi tiết ServiceTask
    /// </summary>
    public class ServiceTaskResponseDto
    {
        public long Id { get; set; }
        public long MaintenanceTicketId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StatusCode { get; set; }
        public string? Note { get; set; }

        // Labor cost fields
        public long? ServiceCategoryId { get; set; }
        public decimal? StandardLaborTime { get; set; }
        public decimal? ActualLaborTime { get; set; }
        public decimal? LaborCost { get; set; }

        // New fields
        public long? TechnicianId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DisplayOrder { get; set; }
        public string? CompletionNote { get; set; }

        // Navigation properties
        public string? MaintenanceTicketCode { get; set; }
        public string? MaintenanceTicketDescription { get; set; }
        public string? CarName { get; set; }
        public string? CustomerName { get; set; }
        public string? TechnicianName { get; set; }
        public string? BranchName { get; set; }
        public string? ServiceCategoryName { get; set; }
        
        // ✅ Danh sách nhiều kỹ thuật viên
        public List<ServiceTaskTechnicianDto>? Technicians { get; set; }
    }
    
    /// <summary>
    /// DTO cho kỹ thuật viên của ServiceTask
    /// </summary>
    public class ServiceTaskTechnicianDto
    {
        public long TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public string? RoleInTask { get; set; } // PRIMARY, ASSISTANT
        public DateTime? AssignedDate { get; set; }
    }

    /// <summary>
    /// DTO cho danh sách ServiceTask (dạng tóm tắt)
    /// </summary>
    public class ServiceTaskListResponseDto
    {
        public long Id { get; set; }
        public long MaintenanceTicketId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StatusCode { get; set; }
        public string? Note { get; set; }

        // Labor cost fields
        public long? ServiceCategoryId { get; set; }
        public decimal? StandardLaborTime { get; set; }
        public decimal? ActualLaborTime { get; set; }
        public decimal? LaborCost { get; set; }

        // New fields
        public long? TechnicianId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DisplayOrder { get; set; }
        public string? CompletionNote { get; set; }

        // Basic info
        public string? MaintenanceTicketCode { get; set; }
        public string? CarName { get; set; }
        public string? CustomerName { get; set; }
        public string? TechnicianName { get; set; }
        public string? BranchName { get; set; }
        public string? ServiceCategoryName { get; set; }
        
        // ✅ Danh sách nhiều kỹ thuật viên
        public List<ServiceTaskTechnicianDto>? Technicians { get; set; }
    }
}