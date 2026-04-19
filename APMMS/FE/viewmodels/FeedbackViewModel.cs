namespace FE.viewmodels
{
    public class FeedbackItemViewModel
    {
        public long Id { get; set; }
        public string? UserName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public long MaintenanceTicketId { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? ParentId { get; set; }
        public List<FeedbackItemViewModel>? Replies { get; set; }
    }
}
