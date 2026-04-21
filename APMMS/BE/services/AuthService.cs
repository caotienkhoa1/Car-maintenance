using BE.DTOs.Auth;
using BE.interfaces;
using BE.repository.IRepository;
using BE.models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BE.services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, JwtService jwtService, EmailService emailService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByUsernameAsync(loginDto.Username);

            if (user == null || !VerifyPassword(loginDto.Password, user.Password))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                };
            }

            // ✅ Check if account is inactive
            if (user.IsDelete == true || user.StatusCode == "INACTIVE")
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên."
                };
            }

            // Tạo Access Token (ngắn hạn) và Refresh Token (dài hạn)
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Username, user.Role?.Name ?? "User", user.RoleId ?? 0, user.BranchId);
            var refreshToken = _jwtService.GenerateRefreshToken(user.Id);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Đăng nhập thành công",
                Token = accessToken,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Username = user.Username,
                RoleName = user.Role?.Name,
                RoleId = user.RoleId,
                BranchId = user.BranchId,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }

        public async Task<LogoutResponseDto> LogoutAsync(string token)
        {
            // optional: token blacklist
            return new LogoutResponseDto
            {
                Success = true,
                Message = "Logout successful"
            };
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var principal = _jwtService.ValidateToken(token);
            return principal != null;
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            // Validate refresh token (cho phép token đã hết hạn một chút để refresh)
            var principal = _jwtService.ValidateToken(refreshToken, allowExpired: true);
            if (principal == null)
                return new LoginResponseDto { Success = false, Message = "Refresh token không hợp lệ" };

            // Kiểm tra xem có phải refresh token không
            var tokenType = principal.FindFirst("token_type")?.Value;
            if (tokenType != "refresh")
                return new LoginResponseDto { Success = false, Message = "Token không phải là refresh token" };

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return new LoginResponseDto { Success = false, Message = "Refresh token không hợp lệ" };

            var user = await _userRepository.GetByIdAsync(long.Parse(userId));
            if (user == null)
                return new LoginResponseDto { Success = false, Message = "Không tìm thấy người dùng" };

            // ✅ Check if account is inactive
            if (user.IsDelete == true || user.StatusCode == "INACTIVE")
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên."
                };
            }

            // Tạo Access Token và Refresh Token mới
            var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Username, user.Role?.Name ?? "User", user.RoleId ?? 0, user.BranchId);
            var newRefreshToken = _jwtService.GenerateRefreshToken(user.Id);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Làm mới token thành công",
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                Username = user.Username,
                RoleName = user.Role?.Name,
                RoleId = user.RoleId,
                BranchId = user.BranchId
            };
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string storedPassword)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == storedPassword || password == storedPassword;
        }

        public async Task<ChangePasswordResponseDto> ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng"
                };
            }

            if (!VerifyPassword(dto.OldPassword, user.Password))
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu hiện tại không đúng"
                };
            }

            user.Password = HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);

            return new ChangePasswordResponseDto
            {
                Success = true,
                Message = "Đổi mật khẩu thành công"
            };
        }

        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            // Tìm user theo email hoặc username
            User? user = null;
            
            if (dto.EmailOrUsername.Contains("@"))
            {
                // Nếu có @ thì tìm theo email
                user = await _userRepository.GetByEmailAsync(dto.EmailOrUsername);
            }
            else
            {
                // Nếu không có @ thì tìm theo username
                user = await _userRepository.GetByUsernameAsync(dto.EmailOrUsername);
            }

            // Luôn trả về success để không tiết lộ thông tin user tồn tại hay không
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                return new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "Nếu email/username tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu."
                };
            }

            // Kiểm tra tài khoản có bị vô hiệu hóa không
            if (user.IsDelete == true || user.StatusCode == "INACTIVE")
            {
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên."
                };
            }

            // Tạo reset token
            var resetToken = GenerateResetToken();
            user.ResetKey = resetToken;
            user.ResetDate = DateTime.UtcNow.AddHours(24); // Token hết hạn sau 24 giờ
            await _userRepository.UpdateAsync(user);

            // Tạo reset URL
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7173";
            var resetUrl = $"{baseUrl}/Auth/ResetPassword?token={resetToken}";

            // Gửi email
            var emailSent = await _emailService.SendPasswordResetEmailAsync(
                user.Email,
                user.Username,
                resetToken,
                resetUrl
            );

            if (!emailSent)
            {
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Không thể gửi email. Vui lòng thử lại sau."
                };
            }

            return new ForgotPasswordResponseDto
            {
                Success = true,
                Message = "Email hướng dẫn đặt lại mật khẩu đã được gửi. Vui lòng kiểm tra hộp thư của bạn."
            };
        }

        public async Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            // Tìm user theo reset token
            var user = await _userRepository.GetByResetTokenAsync(dto.Token);

            if (user == null)
            {
                return new ResetPasswordResponseDto
                {
                    Success = false,
                    Message = "Token không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu đặt lại mật khẩu mới."
                };
            }

            // Cập nhật mật khẩu
            user.Password = HashPassword(dto.NewPassword);
            user.ResetKey = null; // Xóa token sau khi đã sử dụng
            user.ResetDate = null;
            await _userRepository.UpdateAsync(user);

            return new ResetPasswordResponseDto
            {
                Success = true,
                Message = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới."
            };
        }

        private string GenerateResetToken()
        {
            // Tạo token ngẫu nhiên 32 ký tự
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
