using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using FE.services;
using FE.viewmodels;
using Microsoft.Extensions.Configuration;

namespace FE.controllers
{
    [Route("Auth")]
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(AuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("Login")]
        public IActionResult Login()
        {
            return View("~/views/Auth/Login.cshtml");
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel request)
        {
            try
            {
                Console.WriteLine($"Login attempt for user: {request.Username}");
                
                var result = await _authService.LoginAsync(request.Username, request.Password);
                Console.WriteLine($"AuthService result: Success={result?.Success}, Token={result?.Token?.Substring(0, Math.Min(50, result.Token?.Length ?? 0))}...");
                
                if (result?.Success == true)
                {
                    // Store token in session or cookie
                    HttpContext.Session.SetString("AuthToken", result.Token ?? "");
                    HttpContext.Session.SetString("Username", request.Username);
                    HttpContext.Session.SetString("UserId", result.UserId.ToString());
                    
                    // Use roleId from the response
                    var roleId = result.RoleId;
                    HttpContext.Session.SetString("RoleId", roleId.ToString());
                    
                    // Store BranchId in session if available
                    if (result.BranchId.HasValue)
                    {
                        HttpContext.Session.SetString("BranchId", result.BranchId.Value.ToString());
                        Console.WriteLine($"Login: Saved BranchId to session: {result.BranchId.Value}");
                    }
                    else
                    {
                        Console.WriteLine("Login: BranchId not available in response");
                    }
                    
                    // ✅ Lưu FirstName và LastName vào session
                    if (!string.IsNullOrEmpty(result.FirstName))
                    {
                        HttpContext.Session.SetString("FirstName", result.FirstName);
                    }
                    if (!string.IsNullOrEmpty(result.LastName))
                    {
                        HttpContext.Session.SetString("LastName", result.LastName);
                    }
                    
                    Console.WriteLine($"Đăng nhập thành công - user: {request.Username}, Role: {roleId}, UserId: {result.UserId}, BranchId: {result.BranchId}");
                    return Json(new { 
                        success = true, 
                        token = result.Token,
                        userId = result.UserId,
                        roleId = roleId,
                        branchId = result.BranchId,
                        firstName = result.FirstName,
                        lastName = result.LastName,
                        redirectTo = result.RedirectTo ?? GetRedirectUrl(roleId)
                    });
                }
                
                Console.WriteLine($"Login failed for user: {request.Username}, Error: {result?.Error}");
                return Json(new { success = false, error = result?.Error ?? "Đăng nhập thất bại" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login exception: {ex.Message}");
                return Json(new { success = false, error = "Lỗi kết nối: " + ex.Message });
            }
        }

        private string GetRedirectUrl(int roleId)
        {
            return roleId switch
            {
                1 => "/Dashboard", // Admin
                2 => "/Dashboard", // Branch Manager
                3 => "/Dashboard", // Accountant
                4 => "/Dashboard", // Technician
                5 => "/Dashboard", // Warehouse Keeper
                6 => "/Dashboard", // Consulter
                7 => "/", // Auto Owner - stay on home page
                8 => "/", // Guest - stay on home page
                _ => "/"
            };
        }

        [HttpGet]
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            // Clear session
            HttpContext.Session.Clear();
            
            // Sign out cookie
            await HttpContext.SignOutAsync("CookieAuthentication");
            
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Route("Logout")]
        public async Task<IActionResult> LogoutPost()
        {
            // Clear session
            HttpContext.Session.Clear();

            // Sign out cookie
            await HttpContext.SignOutAsync("CookieAuthentication");

            return Json(new { success = true, redirectTo = Url.Action("Login", "Auth") });
        }

        [HttpGet]
        [Route("GetUserInfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            var token = HttpContext.Session.GetString("AuthToken");
            var username = HttpContext.Session.GetString("Username") ?? string.Empty;
            var roleIdString = HttpContext.Session.GetString("RoleId");
            int roleId = 0;
            if (!string.IsNullOrEmpty(roleIdString))
            {
                int.TryParse(roleIdString, out roleId);
            }

            // Lấy branchId từ JWT token nếu có
            long? branchId = null;
            string? firstName = null;
            string? lastName = null;
            string? fullName = null;
            
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var parts = token.Split('.');
                    if (parts.Length == 3)
                    {
                        var payload = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                            System.Convert.FromBase64String(parts[1] + "=="));
                        
                        if (payload != null)
                        {
                            // Thử các key có thể có branchId
                            if (payload.ContainsKey("BranchId"))
                            {
                                if (payload["BranchId"] is System.Text.Json.JsonElement branchIdElement)
                                {
                                    if (branchIdElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    {
                                        branchId = branchIdElement.GetInt64();
                                    }
                                }
                            }
                            else if (payload.ContainsKey("branchId"))
                            {
                                if (payload["branchId"] is System.Text.Json.JsonElement branchIdElement)
                                {
                                    if (branchIdElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    {
                                        branchId = branchIdElement.GetInt64();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decoding branchId from token: {ex.Message}");
                }
                
                // ✅ Gọi BE API để lấy firstName/lastName từ server-side
                var userIdString = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdString) && long.TryParse(userIdString, out var userId))
                {
                    try
                    {
                        // Gọi BE API từ server-side để tránh lỗi 403
                        var backendResponse = await _authService.GetAutoOwnerInfoAsync(userId);
                        if (backendResponse != null)
                        {
                            firstName = backendResponse.FirstName;
                            lastName = backendResponse.LastName;
                            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                            {
                                fullName = $"{firstName} {lastName}".Trim();
                            }
                            else if (!string.IsNullOrEmpty(firstName))
                            {
                                fullName = firstName;
                            }
                            else if (!string.IsNullOrEmpty(lastName))
                            {
                                fullName = lastName;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting AutoOwner info: {ex.Message}");
                        // Không throw, chỉ log lỗi và tiếp tục với thông tin có sẵn
                    }
                }
            }

            var isLoggedIn = !string.IsNullOrEmpty(token);
            
            // ✅ Lấy firstName/lastName từ session (đã lưu khi login)
            var sessionFirstName = HttpContext.Session.GetString("FirstName");
            var sessionLastName = HttpContext.Session.GetString("LastName");
            
            // Ưu tiên dùng từ session, nếu không có thì dùng từ API (nếu đã gọi)
            var finalFirstName = sessionFirstName ?? firstName;
            var finalLastName = sessionLastName ?? lastName;
            var finalFullName = fullName;
            
            if (string.IsNullOrEmpty(finalFullName) && !string.IsNullOrEmpty(finalFirstName) && !string.IsNullOrEmpty(finalLastName))
            {
                finalFullName = $"{finalFirstName} {finalLastName}".Trim();
            }
            
            return Json(new { 
                isLoggedIn, 
                username, 
                roleId,
                branchId = branchId,
                userId = HttpContext.Session.GetString("UserId"),
                firstName = finalFirstName,
                lastName = finalLastName,
                fullName = finalFullName
            });
        }

        [HttpGet]
        [Route("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            ViewBag.ApiBaseUrl = _configuration?["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View();
        }

        [HttpGet]
        [Route("ResetPassword")]
        public IActionResult ResetPassword()
        {
            ViewBag.ApiBaseUrl = _configuration?["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View();
        }
    }
}
