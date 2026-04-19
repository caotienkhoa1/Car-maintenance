namespace BE.DTOs.ServicePackage
{
    public class RequestDto
    {
        public long? Id { get; set; } // null -> create
        public long? BranchId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? StatusCode { get; set; }

        // Components to include in package với số lượng tương ứng
        public List<ComponentPackageDto>? Components { get; set; }
        
        // Backward compatibility: Nếu Components null nhưng ComponentIds có giá trị, sẽ dùng ComponentIds với Quantity = 1
        [System.Obsolete("Use Components instead. This property is kept for backward compatibility.")]
        public List<long>? ComponentIds { get; set; }
    }
    
    public class ComponentPackageDto
    {
        public long ComponentId { get; set; }
        public int Quantity { get; set; } = 1; // Số lượng component trong package (mặc định 1)
    }
}


