namespace BE.DTOs.AutoOwner
{
    public class ResponseDto
    {
        public long Id { get; set; }
        public string? Code { get; set; }
        public string Username { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? Image { get; set; }
        public string? TaxCode { get; set; }
        public string? StatusCode { get; set; }
        public long? RoleId { get; set; }
        public long? BranchId { get; set; }
        public string? Address { get; set; }
        public string? CitizenId { get; set; }
        public string? BranchName { get; set; }
        public string? RoleName { get; set; }
        public DateOnly? Dob { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
