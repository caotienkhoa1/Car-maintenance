using BE.models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BE.extensions
{
    /// <summary>
    /// Middleware để check trạng thái tài khoản INACTIVE khi user đã đăng nhập
    /// </summary>
    public class UserStatusCheckMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserStatusCheckMiddleware> _logger;

        public UserStatusCheckMiddleware(RequestDelegate next, ILogger<UserStatusCheckMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, CarMaintenanceDbContext dbContext)
        {
            // Skip if not authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            // Get user ID from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId))
            {
                await _next(context);
                return;
            }

            // Check user status
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null && (user.IsDelete == true || user.StatusCode == "INACTIVE"))
            {
                // Account is inactive/deleted
                _logger.LogWarning($"Inactive user {userId} attempted to access {context.Request.Path}");
                
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.",
                    code = "ACCOUNT_INACTIVE"
                });
                
                return;
            }

            // User is active, continue
            await _next(context);
        }
    }

    public static class UserStatusCheckMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserStatusCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserStatusCheckMiddleware>();
        }
    }
}

