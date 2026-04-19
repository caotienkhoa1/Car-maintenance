using BE.models;

namespace BE.DTOs.VehicleCheckin
{
    /// <summary>
    /// DTO cho response chi tiết Vehicle Check-in
    /// </summary>
    public class ResponseDto
    {
        public long Id { get; set; }
        public long CarId { get; set; }
        public long MaintenanceRequestId { get; set; }
        public int Mileage { get; set; }
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Code { get; set; }
        
        // Thông tin xe
        public string? CarName { get; set; }
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? VinNumber { get; set; }
        public string? VehicleEngineNumber { get; set; }
        public string? Color { get; set; }
        public int? YearOfManufacture { get; set; }
        public string? VehicleType { get; set; }
        public long? VehicleTypeId { get; set; }
        
        // Thông tin khách hàng
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
        
        // Thông tin chi nhánh
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }
        
        // Thông tin người tạo
        public string? ConsulterName { get; set; }
        
        // Hình ảnh
        public List<VehicleCheckinImageDto> Images { get; set; } = new List<VehicleCheckinImageDto>();
        
        // Thông tin yêu cầu bảo dưỡng
        public string? MaintenanceRequestStatus { get; set; }
        public DateTime? RequestDate { get; set; }
        
        // Trạng thái VehicleCheckin
        public string? StatusCode { get; set; }
    }
    
    /// <summary>
    /// DTO cho hình ảnh Vehicle Check-in
    /// </summary>
    public class VehicleCheckinImageDto
    {
        public long Id { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
    }
    
    /// <summary>
    /// DTO cho danh sách Vehicle Check-in (dạng tóm tắt)
    /// </summary>
    public class ListResponseDto
    {
        public long Id { get; set; }
        public long CarId { get; set; }
        public string? Code { get; set; }
        public string? CarName { get; set; }
        public string? LicensePlate { get; set; }
        public string? VinNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? VehicleType { get; set; }
        public int Mileage { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Notes { get; set; }
        public string? FirstImageUrl { get; set; }
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? MaintenanceRequestStatus { get; set; }
        public string? StatusCode { get; set; }
    }
}


