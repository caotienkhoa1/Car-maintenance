using BE.DTOs.Auth;
using BE.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }


        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 200)]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 400)]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 500)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);

            if (!result.Success)
            {
                return BadRequest(new AuthResponseWrapperDto<object>
                {
                    Success = false,
                    Message = result.Message
                });
            }

            var response = new AuthResponseWrapperDto<object>
            {
                Success = true,
                Message = result.Message,
                Data = new
                {
                    result.Token,
                    result.RefreshToken,
                    result.UserId,
                    result.Username,
                    result.RoleName,
                    result.RoleId,
                    result.BranchId,
                    result.FirstName,
                    result.LastName
                }
            };

            return Ok(response);
        }


        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 200)]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 400)]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 500)]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new AuthResponseWrapperDto<object>
                {
                    Success = false,
                    Message = "Token not provided"
                });
            }

            var result = await _authService.LogoutAsync(token);

            return Ok(new AuthResponseWrapperDto<object>
            {
                Success = true,
                Message = result.Message
            });
        }


        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 200)]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 400)]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 500)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.Token);

            if (!result.Success)
            {
                return BadRequest(new AuthResponseWrapperDto<object>
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(new AuthResponseWrapperDto<object>
            {
                Success = true,
                Message = result.Message,
                Data = new
                {
                    result.Token,
                    result.RefreshToken,
                    result.UserId,
                    result.Username,
                    result.RoleName,
                    result.RoleId,
                    result.BranchId
                }
            });
        }


        /// <summary>
        /// Kiểm tra token có hợp lệ không
        /// </summary>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 200)]
        [ProducesResponseType(typeof(AuthResponseWrapperDto<object>), 500)]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenDto dto)
        {
            var isValid = await _authService.ValidateTokenAsync(dto.Token);

            return Ok(new AuthResponseWrapperDto<object>
            {
                Success = true,
                Message = isValid ? "Token hợp lệ" : "Token không hợp lệ",
                Data = new { isValid }
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ChangePasswordResponseDto), 200)]
        [ProducesResponseType(typeof(ChangePasswordResponseDto), 400)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var result = await _authService.ChangePasswordAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ForgotPasswordResponseDto), 200)]
        [ProducesResponseType(typeof(ForgotPasswordResponseDto), 400)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ResetPasswordResponseDto), 200)]
        [ProducesResponseType(typeof(ResetPasswordResponseDto), 400)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }


    }
}
