using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace FE.controllers
{
    [Route("Employees")]
    public class EmployeeController : Controller
    {
        private readonly IConfiguration _configuration;

        public EmployeeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        /// <summary>
        /// ✅ Kiểm tra quyền truy cập: Chỉ Admin (1) và Branch Manager (2) mới được phép
        /// </summary>
        private IActionResult? CheckAuthorization()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để truy cập trang này.";
                return RedirectToAction("Login", "Auth");
            }

            var roleIdInt = int.Parse(roleId);
            if (roleIdInt != 1 && roleIdInt != 2)
            {
                // Trả 403 để hiển thị lỗi rõ ràng thay vì chuyển về trang Home
                return StatusCode(403, "Bạn không có quyền truy cập trang này.");
            }

            return null;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            var authCheck = CheckAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View();
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            var authCheck = CheckAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View();
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var authCheck = CheckAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            ViewBag.EmployeeId = id;
            return View();
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            var authCheck = CheckAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            ViewBag.EmployeeId = id;
            return View();
        }
    }
}
