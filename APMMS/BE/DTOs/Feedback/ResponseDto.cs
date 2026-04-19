namespace BE.DTOs.Feedback
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Image { get; set; }

        public long? MaintenanceTicketId { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? ParentId { get; set; }
    }
}
