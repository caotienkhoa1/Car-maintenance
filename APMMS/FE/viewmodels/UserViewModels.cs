using System.ComponentModel.DataAnnotations;

namespace FE.viewmodels
{
    // User ViewModel
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    // Request Models
    public class CreateUserRequestModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        public int RoleId { get; set; } = 7; // Default to Auto Owner
    }

    public class UpdateUserRequestModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
    }

    // Response Models
    public class UserResponseModel
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public UserModel? Data { get; set; }
    }

    public class DeleteUserResponseModel
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
