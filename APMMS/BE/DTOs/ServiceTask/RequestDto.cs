using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.ServiceTask
{
    /// <summary>
    /// DTO cho việc tạo mới ServiceTask
    /// </summary>
    public class ServiceTaskRequestDto
    {
        [Required(ErrorMessage = "Maintenance Ticket ID is required")]
        public long MaintenanceTicketId { get; set; }

        [Required(ErrorMessage = "Task name is required")]
        [StringLength(100, ErrorMessage = "Task name cannot exceed 100 characters")]
        public string TaskName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Status code cannot exceed 50 characters")]
        public string? StatusCode { get; set; } = "PENDING";

        [StringLength(255, ErrorMessage = "Note cannot exceed 255 characters")]
        public string? Note { get; set; }

        // Labor cost fields
        public long? ServiceCategoryId { get; set; } // Tham chiếu đến ServiceCategory (catalog)
        public decimal? StandardLaborTime { get; set; } // Thời gian chuẩn (giờ)
        public decimal? ActualLaborTime { get; set; } // Thời gian thực tế (có thể chỉnh sửa)
        
        // New fields
        public long? TechnicianId { get; set; } // Kỹ thuật viên được gán
        public int? DisplayOrder { get; set; } // Thứ tự hiển thị
    }

    /// <summary>
    /// DTO cho việc cập nhật ServiceTask
    /// </summary>
    public class ServiceTaskUpdateDto
    {
        [Required(ErrorMessage = "ID is required")]
        public long Id { get; set; }

        [Required(ErrorMessage = "Maintenance Ticket ID is required")]
        public long MaintenanceTicketId { get; set; }

        [Required(ErrorMessage = "Task name is required")]
        [StringLength(100, ErrorMessage = "Task name cannot exceed 100 characters")]
        public string TaskName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Status code cannot exceed 50 characters")]
        public string? StatusCode { get; set; }

        [StringLength(255, ErrorMessage = "Note cannot exceed 255 characters")]
        public string? Note { get; set; }

        // Labor cost fields
        public long? ServiceCategoryId { get; set; }
        public decimal? StandardLaborTime { get; set; }
        public decimal? ActualLaborTime { get; set; }
        
        // New fields
        public long? TechnicianId { get; set; } // Kỹ thuật viên được gán
        public int? DisplayOrder { get; set; } // Thứ tự hiển thị
        public string? CompletionNote { get; set; } // Ghi chú khi hoàn thành
    }

    /// <summary>
    /// DTO cho việc cập nhật thời gian lao động của ServiceTask
    /// </summary>
    public class ServiceTaskUpdateLaborTimeDto
    {
        [Required(ErrorMessage = "ID is required")]
        public long Id { get; set; }

        [Range(0.01, 999.99, ErrorMessage = "Actual labor time must be between 0.01 and 999.99 hours")]
        public decimal ActualLaborTime { get; set; }
    }

    /// <summary>
    /// DTO cho việc cập nhật status của ServiceTask
    /// </summary>
    public class ServiceTaskUpdateStatusDto
    {
        [Required(ErrorMessage = "Status code is required")]
        [StringLength(50, ErrorMessage = "Status code cannot exceed 50 characters")]
        public string StatusCode { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Completion note cannot exceed 500 characters")]
        public string? CompletionNote { get; set; } // Ghi chú khi hoàn thành (bắt buộc nếu status = DONE)
    }
    
    /// <summary>
    /// DTO cho việc gán kỹ thuật viên cho ServiceTask
    /// </summary>
    public class ServiceTaskAssignTechniciansDto
    {
        [Required(ErrorMessage = "Technician IDs are required")]
        public List<long> TechnicianIds { get; set; } = new List<long>();
        
        public long? PrimaryTechnicianId { get; set; } // Kỹ thuật viên chính (tùy chọn)
    }
}