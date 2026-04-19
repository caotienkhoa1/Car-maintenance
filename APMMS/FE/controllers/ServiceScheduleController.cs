using Microsoft.AspNetCore.Mvc;
using FE.services;

namespace FE.controllers
{
    [Route("ServiceSchedules")]
    public class ServiceScheduleController : Controller
    {
        private readonly ServiceScheduleService _service;
        private readonly IConfiguration _configuration;

        public ServiceScheduleController(ServiceScheduleService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View("~/views/ServiceSchedules/Index.cshtml");
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View("~/views/ServiceSchedules/Create.cshtml");
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            ViewBag.ServiceScheduleId = id;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View("~/views/ServiceSchedules/Edit.cshtml");
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            // ✅ Kiểm tra quyền truy cập
            var roleIdStr = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!int.TryParse(roleIdStr, out var roleId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // ✅ Chặn AutoOwner (role 7) - khách hàng không được xem chi tiết
            if (roleId == 7)
            {
                return RedirectToAction("Index", "Profile");
            }

            // ✅ Chặn Guest (role 8)
            if (roleId == 8)
            {
                return RedirectToAction("Index", "Home");
            }

            // ✅ Chỉ cho phép nhân viên (role 1-6) vào trang Details
            if (roleId < 1 || roleId > 6)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.ServiceScheduleId = id;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View("~/views/ServiceSchedules/Details.cshtml");
        }

        [HttpGet]
        [Route("ListData")]
        public async Task<IActionResult> ListData(int page = 1, int pageSize = 10, string? status = null, string? date = null)
        {
            try
            {
                object? data;
                if (!string.IsNullOrEmpty(status))
                {
                    data = await _service.GetByStatusAsync(status);
                }
                else if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime dateValue))
                {
                    var startDate = dateValue.Date;
                    var endDate = startDate.AddDays(1);
                    data = await _service.GetByDateRangeAsync(startDate, endDate);
                }
                else
                {
                    data = await _service.GetAllAsync(page, pageSize);
                }
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
