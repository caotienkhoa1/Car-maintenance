namespace BE.DTOs.ServicePackage
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? StatusCode { get; set; }

        // Summary of included components
        public List<ComponentSummary>? Components { get; set; }

        // Summary of included service categories (tasks)
        public List<ServiceCategorySummary>? ServiceCategories { get; set; }

        public class ComponentSummary
        {
            public long Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public decimal? UnitPrice { get; set; }
            public int? QuantityStock { get; set; }
            public string? ImageUrl { get; set; }
            public int? Quantity { get; set; } // Số lượng trong gói dịch vụ (nếu có trong bảng trung gian)
            public string? StatusCode { get; set; } // Trạng thái của linh kiện
        }

        public class ServiceCategorySummary
        {
            public long Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public decimal? StandardLaborTime { get; set; } // Thời gian chuẩn từ ServicePackageCategory (có thể override)
        }
    }
}


