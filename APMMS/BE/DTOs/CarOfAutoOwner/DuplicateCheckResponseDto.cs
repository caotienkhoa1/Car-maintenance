namespace BE.DTOs.CarOfAutoOwner
{
    public class DuplicateCheckResponseDto
    {
        public bool LicensePlateExists { get; set; }
        public bool VinNumberExists { get; set; }
        public bool EngineNumberExists { get; set; }
    }
}

