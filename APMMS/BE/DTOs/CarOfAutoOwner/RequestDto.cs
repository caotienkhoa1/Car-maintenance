namespace BE.DTOs.CarOfAutoOwner
{
    public class RequestDto
    {
        public long? UserId { get; set; }
        public string? CarName { get; set; }
        public string? CarModel { get; set; }
        public long? VehicleTypeId { get; set; }
        public string? Color { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleEngineNumber { get; set; }
        public string? VinNumber { get; set; }
        public int? YearOfManufacture { get; set; }
        public long? BranchId { get; set; }
    }
}
