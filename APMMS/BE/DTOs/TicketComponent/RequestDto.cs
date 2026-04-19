using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.TicketComponent
{
    public class RequestDto
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "Maintenance Ticket ID is required")]
        public long MaintenanceTicketId { get; set; }

        [Required(ErrorMessage = "Component ID is required")]
        public long ComponentId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Actual quantity must be greater than or equal to 0")]
        public decimal? ActualQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be greater than or equal to 0")]
        public decimal? UnitPrice { get; set; }
        
        // ✅ Đánh dấu phụ tùng từ gói dịch vụ (chỉ dùng khi tạo từ ApplyServicePackage)
        public long? ServicePackageId { get; set; }
    }
}

