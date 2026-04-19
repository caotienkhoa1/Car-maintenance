namespace BE.DTOs.ServiceSchedule
{
    public class AddNoteDto
    {
        public long ConsultantId { get; set; }

        public string Note { get; set; } = null!;
    }
}

