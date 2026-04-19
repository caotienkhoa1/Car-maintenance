using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.ServiceSchedule
{
    public class RequestDto
    {
        [Required(ErrorMessage = "User ID is required")]
        public long UserId { get; set; }

        [Required(ErrorMessage = "Car ID is required")]
        public long CarId { get; set; }

        [Required(ErrorMessage = "Scheduled date is required")]
        public DateTime ScheduledDate { get; set; }

        [Required(ErrorMessage = "Branch ID is required")]
        public long BranchId { get; set; }

        public long? ServiceCategoryId { get; set; }

        [StringLength(50, ErrorMessage = "Status code cannot exceed 50 characters")]
        public string? StatusCode { get; set; } = "PENDING"; // Default status: PENDING, CONFIRMED, CANCELLED, COMPLETED
    }

    public class UpdateScheduleDto
    {
        public DateTime? ScheduledDate { get; set; }
        public long? BranchId { get; set; }
        public long? ServiceCategoryId { get; set; }
        public string? StatusCode { get; set; }
    }

    public class CancelScheduleDto
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO cho khách hàng đặt lịch công khai (chưa có tài khoản)
    /// </summary>
    public class PublicBookingDto
    {
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Car name is required")]
        public string CarName { get; set; } = string.Empty;

        public string? LicensePlate { get; set; }

        public string? CarModel { get; set; }

        public int? Mileage { get; set; }

        [Required(ErrorMessage = "Scheduled date is required")]
        public DateTime ScheduledDate { get; set; }

        [Required(ErrorMessage = "Branch ID is required")]
        public long BranchId { get; set; }

        public string? Message { get; set; }

        public string? ServiceType { get; set; }

        public long? ServiceCategoryId { get; set; }
    }
}
