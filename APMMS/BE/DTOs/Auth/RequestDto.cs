using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.Auth
{
    /// <summary>
    /// DTO cho Login
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho Register
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public long? RoleId { get; set; }
    }
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "UserId is required")]
        public long UserId { get; set; }

        [Required(ErrorMessage = "Old password is required")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(9, ErrorMessage = "New password must be at least 9 characters")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).{9,}$", ErrorMessage = "New password must include at least one uppercase letter and one number")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho Forgot Password
    /// </summary>
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email or Username is required")]
        public string EmailOrUsername { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho Reset Password
    /// </summary>
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(9, ErrorMessage = "New password must be at least 9 characters")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).{9,}$", ErrorMessage = "New password must include at least one uppercase letter and one number")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
