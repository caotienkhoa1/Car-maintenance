namespace BE.DTOs.Component
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? PurchasePrice { get; set; }
        public int? QuantityStock { get; set; }
        public int? MinimumQuantity { get; set; } // Số lượng tối thiểu để cảnh báo
        public long? BranchId { get; set; }
        public long? TypeComponentId { get; set; }
        public string? StatusCode { get; set; }

        // optional nested info
        public string? TypeComponentName { get; set; }
        public string? BranchName { get; set; }
    }
}