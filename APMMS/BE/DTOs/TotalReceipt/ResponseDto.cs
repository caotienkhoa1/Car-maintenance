using System;

namespace BE.DTOs.TotalReceipt
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public long? MaintenanceTicketId { get; set; }
        public string? MaintenanceTicketCode { get; set; }
        public long? CarId { get; set; }
        public string? CarName { get; set; }
        public string? CarModel { get; set; }
        public string? VehicleType { get; set; }
        public string? LicensePlate { get; set; }
        public string? VinNumber { get; set; }
        public string? EngineNumber { get; set; }
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchAddress { get; set; }
        public string? BranchPhone { get; set; }
        public long? AccountantId { get; set; }
        public string? AccountantName { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? VatPercent { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? SurchargeAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public string? CurrencyCode { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        
        // Customer info
        public string? CustomerName { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerGender { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
        
        // Maintenance ticket timeline
        public DateTime? MaintenanceStartTime { get; set; }
        public DateTime? MaintenanceEndTime { get; set; }
        
        // Service Package fields
        public long? ServicePackageId { get; set; }
        public decimal? ServicePackagePrice { get; set; }
        public string? ServicePackageName { get; set; }
        public decimal? PackageDiscountAmount { get; set; }
    }
}


