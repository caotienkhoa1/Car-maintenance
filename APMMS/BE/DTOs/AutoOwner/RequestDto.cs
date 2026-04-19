namespace BE.DTOs.AutoOwner
{
    public class RequestDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = "123456"; // default, có thể hash lại
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? Image { get; set; }
        public string? TaxCode { get; set; }
        public long? BranchId { get; set; }
        public string? Address { get; set; }
        public string? CitizenId { get; set; }
        public DateOnly? Dob { get; set; }
    }
}
