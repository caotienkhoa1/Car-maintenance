namespace BE.DTOs.Feedback
{
    public class RequestDto
    {
        public long? UserId { get; set; }
        public long? MaintenanceTicketId { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public long? ParentId { get; set; }
    }
}
