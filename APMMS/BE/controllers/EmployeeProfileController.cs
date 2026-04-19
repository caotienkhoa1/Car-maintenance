using BE.DTOs.Employee;
using BE.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace BE.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public EmployeeProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>
        /// Lấy thông tin profile của chính employee (dựa trên userId từ token)
        /// Cho phép tất cả user đã đăng nhập xem profile của chính mình
        /// </summary>
        [HttpGet("my-profile")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ: không tìm thấy UserId" });
                }

                var employee = await _profileService.GetMyProfileAsync(userId);
                if (employee == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy người dùng" });
                }

                return Ok(employee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật profile của chính employee (dựa trên userId từ token)
        /// Cho phép employee cập nhật thông tin của chính mình (không cho phép thay đổi roleId, branchId, statusCode)
        /// </summary>
        [HttpPut("my-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromBody] EmployeeProfileUpdateDto dto)
        {
            try
            {
                // Validate model - EmployeeProfileUpdateDto không có Username, Password, RoleId, BranchId, StatusCode
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                        .SelectMany(ms => ms.Value.Errors.Select(e => 
                            string.IsNullOrEmpty(e.ErrorMessage) 
                                ? $"{ms.Key}: {e.Exception?.Message ?? "Invalid value"}" 
                                : e.ErrorMessage))
                        .ToList();
                    
                    // Nếu không có error message nào, thêm message mặc định
                    if (errors.Count == 0)
                    {
                        errors.Add("Dữ liệu không hợp lệ");
                    }
                    
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
                }
                
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ: không tìm thấy UserId" });
                }

                var result = await _profileService.UpdateMyProfileAsync(userId, dto);

                return Ok(new { success = true, data = result, message = "Profile updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Upload avatar image to Cloudinary for current user (dựa trên userId từ token)
        /// </summary>
        [HttpPost("upload-avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ: không tìm thấy UserId" });
                }

                var imageUrl = await _profileService.UploadAvatarAsync(userId, file);

                return Ok(new { success = true, data = new { imageUrl = imageUrl }, message = "Upload avatar thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }
}

