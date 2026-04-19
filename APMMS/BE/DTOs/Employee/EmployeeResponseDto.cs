namespace BE.DTOs.Employee
{
    public class EmployeeResponseDto
    {
        public long Id { get; set; }
        public string? Code { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? Image { get; set; }
        public string? RoleName { get; set; }
        public long? RoleId { get; set; }
        public long? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool? IsDelete { get; set; }
        public string? TaxCode { get; set; }
        public string? FullAddress { get; set; }
        public DateTime? Dob { get; set; }
        public string? CitizenId { get; set; }
        public string? StatusCode { get; set; }
    }
}
