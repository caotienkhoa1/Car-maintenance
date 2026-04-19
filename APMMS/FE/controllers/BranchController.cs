using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FE.controllers
{
    [Route("Branches")]
    public class BranchController : Controller
    {
        private readonly IConfiguration _configuration;

        public BranchController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        /// <summary>
        /// ✅ Kiểm tra quyền truy cập: Chỉ Admin (role id = 1) mới được phép
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
            if (roleIdInt != 1)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này. Chỉ Admin mới được phép.";
                return RedirectToAction("Index", "Home");
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
            return View("~/views/Branch/Index.cshtml");
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(long id)
        {
            var authCheck = CheckAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            ViewBag.BranchId = id;
            return View("~/views/Branch/Edit.cshtml");
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(long id)
        {
            var authCheck = CheckAuthorization();
            if (authCheck != null) return authCheck;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            ViewBag.BranchId = id;
            return View("~/views/Branch/Details.cshtml");
        }
    }
}

