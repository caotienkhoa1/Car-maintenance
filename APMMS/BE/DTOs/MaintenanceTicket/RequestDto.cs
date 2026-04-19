using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.MaintenanceTicket
{
    public class RequestDto
    {
        [Required(ErrorMessage = "Car ID is required")]
        public long CarId { get; set; }

        [Required(ErrorMessage = "Consulter ID is required")]
        public long ConsulterId { get; set; }

        public long? TechnicianId { get; set; }

        [Required(ErrorMessage = "Branch ID is required")]
        public long BranchId { get; set; }

        public long? ScheduleServiceId { get; set; }

        public long? ServiceCategoryId { get; set; }

        [StringLength(50, ErrorMessage = "Status code cannot exceed 50 characters")]
        public string? StatusCode { get; set; } = "PENDING"; // Default status

        [StringLength(20)]
        public string? PriorityLevel { get; set; } // LOW | NORMAL | HIGH | URGENT
    }

    /// <summary>
    /// DTO để tạo Maintenance Ticket từ Vehicle Check-in
    /// </summary>
    public class CreateFromCheckinDto
    {
        [Required(ErrorMessage = "Vehicle Check-in ID is required")]
        public long VehicleCheckinId { get; set; }

        [Required(ErrorMessage = "Consulter ID is required")]
        public long ConsulterId { get; set; }

        public long? TechnicianId { get; set; } // Kỹ thuật viên chính (giữ để tương thích)

        public List<long>? TechnicianIds { get; set; } // Danh sách kỹ thuật viên

        [Required(ErrorMessage = "Branch ID is required")]
        public long BranchId { get; set; }

        public long? ScheduleServiceId { get; set; }

        public long? ServiceCategoryId { get; set; }

        [StringLength(50, ErrorMessage = "Status code cannot exceed 50 characters")]
        public string? StatusCode { get; set; } = "PENDING";

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? PriorityLevel { get; set; } // LOW | NORMAL | HIGH | URGENT
    }

    /// <summary>
    /// DTO để cập nhật status của Maintenance Ticket
    /// </summary>
    public class UpdateStatusDto
    {
        [Required(ErrorMessage = "Status code is required")]
        [StringLength(50, ErrorMessage = "Status code cannot exceed 50 characters")]
        public string StatusCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO để gán kỹ thuật viên cho Maintenance Ticket
    /// </summary>
    public class AssignTechnicianDto
    {
        [Required(ErrorMessage = "Technician ID is required")]
        public long TechnicianId { get; set; }
    }

    public class AssignTechniciansDto
    {
        public List<long>? TechnicianIds { get; set; }
        public long? PrimaryId { get; set; }
    }
}


