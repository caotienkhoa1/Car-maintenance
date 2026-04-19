using System.Text.Json.Serialization;

namespace FE.viewmodels
{
    public class CarOfAutoOwnerViewModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("carName")]
        public string? CarName { get; set; }

        [JsonPropertyName("carModel")]
        public string? CarModel { get; set; }

        [JsonPropertyName("vehicleTypeId")]
        public int? VehicleTypeId { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("licensePlate")]
        public string? LicensePlate { get; set; }

        [JsonPropertyName("vehicleEngineNumber")]
        public string? VehicleEngineNumber { get; set; }

        [JsonPropertyName("vinNumber")]
        public string? VinNumber { get; set; }

        [JsonPropertyName("yearOfManufacture")]
        public int? YearOfManufacture { get; set; }

        [JsonPropertyName("branchId")]
        public int? BranchId { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get; set; }

        [JsonPropertyName("lastModifiedDate")]
        public DateTime? LastModifiedDate { get; set; }
    }
}
 