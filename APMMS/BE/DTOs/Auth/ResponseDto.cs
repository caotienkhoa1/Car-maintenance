namespace BE.DTOs.Auth
{
    /// <summary>
    /// DTO cho Login Response
    /// </summary>
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; } // Access Token
        public string? RefreshToken { get; set; } // Refresh Token
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public string? RoleName { get; set; }
        public long? RoleId { get; set; }
        public long? BranchId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    /// <summary>
    /// DTO cho Register Response
    /// </summary>
    public class RegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string? Username { get; set; }
    }

    /// <summary>
    /// DTO cho Logout Response
    /// </summary>
    public class LogoutResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }


    public class ChangePasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho Forgot Password Response
    /// </summary>
    public class ForgotPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho Reset Password Response
    /// </summary>
    public class ResetPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
