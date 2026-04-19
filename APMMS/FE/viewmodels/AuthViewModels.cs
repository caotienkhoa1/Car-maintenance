using System.ComponentModel.DataAnnotations;

namespace FE.viewmodels
{
    // Request Models
    public class LoginRequestModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // Response Models
    public class LoginResponseModel
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public int RoleId { get; set; }
        public long? BranchId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? RedirectTo { get; set; }
        public string? Error { get; set; }
    }

    // Backend API Response Wrapper
    public class BackendApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    // Backend Login Data
    public class BackendLoginData
    {
        public string? Token { get; set; }
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public string? RoleName { get; set; }
        public long? RoleId { get; set; }
        public long? BranchId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

}
