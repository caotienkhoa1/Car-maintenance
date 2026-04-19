namespace FE.viewmodels
{
    public class HomeViewModel
    {
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime CurrentDate { get; set; } = DateTime.Now;
    }
}
