namespace BE.DTOs.TicketComponent
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public long MaintenanceTicketId { get; set; }
        public long ComponentId { get; set; }
        public int Quantity { get; set; }
        public decimal? ActualQuantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }

        // Component info
        public string? ComponentName { get; set; }
        public string? ComponentCode { get; set; }
        public string? ComponentImageUrl { get; set; }
        public string? TypeComponentName { get; set; }
        
        // ✅ Đánh dấu phụ tùng từ gói dịch vụ
        public long? ServicePackageId { get; set; }
        public bool IsFromServicePackage => ServicePackageId.HasValue;
    }
}

