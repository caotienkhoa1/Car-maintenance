using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.VehicleCheckin
{
    /// <summary>
    /// DTO cho việc tạo mới Vehicle Check-in
    /// </summary>
    public class VehicleCheckinRequestDto
    {
        [Required(ErrorMessage = "Car ID is required")]
        public long CarId { get; set; }
        
        /// <summary>
        /// Maintenance Request ID - có thể null nếu chưa tạo phiếu bảo dưỡng
        /// </summary>
        public long? MaintenanceRequestId { get; set; }
        
        [Required(ErrorMessage = "Mileage is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Mileage must be a positive number")]
        public int Mileage { get; set; }
        
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
        
        [Required(ErrorMessage = "At least one image is required")]
        public List<string> ImageUrls { get; set; } = new List<string>();
        
        /// <summary>
        /// Danh sách mô tả cho từng ảnh (tương ứng với ImageUrls)
        /// </summary>
        public List<string>? ImageDescriptions { get; set; }
        
        /// <summary>
        /// Danh sách phân loại cho từng ảnh (tương ứng với ImageUrls)
        /// </summary>
        public List<string>? ImageCategories { get; set; }
        
        [Required(ErrorMessage = "Branch ID is required")]
        public long BranchId { get; set; }
        
        public string? Code { get; set; }

        // Cho phép override thông tin xe / khách hàng ngay tại thời điểm check-in
        public string? LicensePlate { get; set; }
        public string? VinNumber { get; set; }
        public string? EngineNumber { get; set; }
        public string? CarName { get; set; }
        public string? CarModel { get; set; }
        public string? VehicleTypeName { get; set; }
        public long? VehicleTypeId { get; set; }
        public string? CarColor { get; set; }
        public int? YearOfManufacture { get; set; }
        
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
    }
    
    /// <summary>
    /// DTO cho việc cập nhật Vehicle Check-in
    /// </summary>
    public class UpdateDto
    {
        [Required(ErrorMessage = "ID is required")]
        public long Id { get; set; }
        
        [Required(ErrorMessage = "Mileage is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Mileage must be a positive number")]
        public int Mileage { get; set; }
        
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
        
        public List<string> ImageUrls { get; set; } = new List<string>();
        
        /// <summary>
        /// Danh sách mô tả cho từng ảnh (tương ứng với ImageUrls)
        /// </summary>
        public List<string>? ImageDescriptions { get; set; }
        
        /// <summary>
        /// Danh sách phân loại cho từng ảnh (tương ứng với ImageUrls)
        /// </summary>
        public List<string>? ImageCategories { get; set; }
    }
}


