namespace BE.DTOs.TypeComponent
{
    public class RequestDto
    {
        public long? Id { get; set; } // null for create
        public string? Name { get; set; }
        public string? Description { get; set; }
        public long? BranchId { get; set; }
        public string? StatusCode { get; set; }
        
        // Danh sách Component IDs để thêm vào TypeComponent này khi update
        // Nếu có giá trị, sẽ cập nhật TypeComponentId của các components này
        public List<long>? ComponentIds { get; set; }
    }
}