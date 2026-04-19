namespace BE.DTOs.MaintenanceTicket
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public long? ScheduleServiceId { get; set; }
        public long? CarId { get; set; }
        public long? ConsulterId { get; set; }
        public long? TechnicianId { get; set; }
        public string? StatusCode { get; set; }
        public long? BranchId { get; set; }
        public string? Code { get; set; }
        public decimal? TotalEstimatedCost { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? PriorityLevel { get; set; }
        public string? Description { get; set; }
        public long? ServiceCategoryId { get; set; }
        public string? ServiceCategoryName { get; set; }
        
        // Navigation properties
        public string? CarName { get; set; }
        public string? ConsulterName { get; set; }
        public string? TechnicianName { get; set; } // Kỹ thuật viên chính (giữ để tương thích)
        public string? BranchName { get; set; }
        public string? BranchAddress { get; set; }
        public string? ScheduleServiceName { get; set; }
        
        // Danh sách tất cả kỹ thuật viên
        public List<TechnicianInfoDto>? Technicians { get; set; }
        
        // Customer info
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerEmail { get; set; }
        
        // Vehicle info
        public string? LicensePlate { get; set; }
        public string? CarModel { get; set; }
        public string? VinNumber { get; set; }
        public string? VehicleEngineNumber { get; set; }
        public int? YearOfManufacture { get; set; }
        public string? VehicleType { get; set; }
        public long? VehicleTypeId { get; set; }
        public string? Color { get; set; }
        
        // Vehicle Check-in info (nếu được tạo từ check-in)
        public long? VehicleCheckinId { get; set; }
        public int? Mileage { get; set; }
        public string? CheckinNotes { get; set; }
        public List<string>? CheckinImages { get; set; }
        
        // Service Package info
        public long? ServicePackageId { get; set; }
        public string? ServicePackageName { get; set; }
        public decimal? ServicePackagePrice { get; set; }
    }

    public class TechnicianInfoDto
    {
        public long TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public string? RoleInTicket { get; set; } // PRIMARY hoặc ASSISTANT
        public DateTime? AssignedDate { get; set; }
    }

    public class ListResponseDto
    {
        public long Id { get; set; }
        public long? CarId { get; set; }
        public long? ConsulterId { get; set; }
        public long? TechnicianId { get; set; }
        public string? StatusCode { get; set; }
        public long? BranchId { get; set; }
        
        // Basic info
        public string? Code { get; set; }
        public string? CarName { get; set; }
        public string? ConsulterName { get; set; }
        public string? TechnicianName { get; set; }
        public string? BranchName { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class UserMaintenanceHistoryDto
    {
        public long TicketId { get; set; }
        public string? TicketCode { get; set; }
        public string? StatusCode { get; set; }
        public string StatusName { get; set; } = "Không xác định";
        public DateTime? CreatedDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? CarName { get; set; }
        public string? LicensePlate { get; set; }
        public string? BranchName { get; set; }
        public long? ServiceCategoryId { get; set; }
        public string? ServiceCategoryName { get; set; }
        public decimal? ReceiptAmount { get; set; }
        public long? ReceiptId { get; set; }
        public string? ReceiptCode { get; set; }
        public string? ReceiptStatus { get; set; }
    }
}


