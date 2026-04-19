namespace BE.DTOs.Component
{
    public class RequestDto
    {
        public long? Id { get; set; } // null => create
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        
        // ⚠️ Giá và số lượng: Chỉ dùng trong module "Nhập kho", không dùng khi tạo Component mới
        // Khi tạo Component mới, các trường này sẽ bị bỏ qua
        public decimal? UnitPrice { get; set; }
        public decimal? PurchasePrice { get; set; }
        public int? QuantityStock { get; set; }
        
        public int? MinimumQuantity { get; set; } // Số lượng tối thiểu để cảnh báo
        public long? BranchId { get; set; }
        public long? TypeComponentId { get; set; }
        public string? StatusCode { get; set; }
    }
}