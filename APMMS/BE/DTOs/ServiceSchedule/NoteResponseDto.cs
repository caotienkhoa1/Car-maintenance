namespace BE.DTOs.ServiceSchedule
{
    public class NoteResponseDto
    {
        public long Id { get; set; }

        public long ScheduleServiceId { get; set; }

        public long ConsultantId { get; set; }

        public string ConsultantName { get; set; } = null!;

        public string Note { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public bool IsAssignmentNote { get; set; }
    }
}

