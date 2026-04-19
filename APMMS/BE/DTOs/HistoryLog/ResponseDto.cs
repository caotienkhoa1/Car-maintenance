namespace BE.DTOs.HistoryLog
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Action { get; set; }
        public string? NewData { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}


