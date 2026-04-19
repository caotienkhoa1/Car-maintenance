using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace FE.controllers
{
    [Route("Users")]
    public class UserController : Controller
    {
        private readonly IConfiguration _configuration;

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Chỉ cho phép Admin (1), Branch Manager (2), Consulter (6) xem danh sách/chi tiết khách hàng
        /// </summary>
        private IActionResult? CheckViewAuthorization()
        {
            var roleIdStr = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleIdStr))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để truy cập trang này.";
                return RedirectToAction("Login", "Auth");
            }

            if (!int.TryParse(roleIdStr, out var roleId) || (roleId != 1 && roleId != 2 && roleId != 6))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        /// <summary>
        /// Chỉ cho phép Consulter (6) tạo/sửa khách hàng
        /// </summary>
        private IActionResult? CheckEditAuthorization()
        {
            var roleIdStr = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleIdStr) || !int.TryParse(roleIdStr, out var roleId) || roleId != 6)
            {
                TempData["ErrorMessage"] = "Chỉ nhân viên tư vấn mới được quyền chỉnh sửa khách hàng.";
                return RedirectToAction("Index");
            }
            return null;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            var authCheck = CheckViewAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View();
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            var authCheck = CheckEditAuthorization();
            if (authCheck != null) return authCheck;
            return View();
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var authCheck = CheckEditAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.UserId = id;
            return View();
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            var authCheck = CheckViewAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.UserId = id;
            return View();
        }
        [HttpGet]
        [Route("Car/{id}")]
        public IActionResult Car(int id)
        {
            var authCheck = CheckViewAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.UserId = id;
            return View();
        }

        [HttpGet]
        [Route("CarDetail/{carId}")]
        public IActionResult CarDetail(long carId)
        {
            var authCheck = CheckViewAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.CarId = carId;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View();
        }

    }
}
