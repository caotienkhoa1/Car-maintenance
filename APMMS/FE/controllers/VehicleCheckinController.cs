using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FE.services;

namespace FE.controllers
{
    // [Authorize] // Tạm thời comment để test
    [Route("VehicleCheckins")]
    public class VehicleCheckinController : Controller
    {
        private readonly VehicleCheckinService _service;

        public VehicleCheckinController(VehicleCheckinService service)
        {
            _service = service;
        }
        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
			return View("~/views/VehicleCheckins/Index.cshtml");
        }

        [HttpGet]
        [Route("ListData")]
        public async Task<IActionResult> ListData(int page = 1, int pageSize = 10, string? searchTerm = null, string? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            // ✅ Mặc định chỉ hiển thị check-in đã xác nhận (CONFIRMED) nếu không có statusCode
            if (string.IsNullOrWhiteSpace(statusCode))
            {
                statusCode = "CONFIRMED";
            }

            // BE sẽ tự động lấy branchId từ JWT token, không cần FE gửi lên
            var data = await _service.GetAllAsync(page, pageSize, searchTerm, statusCode, fromDate, toDate);
            return Json(data);
        }

        [HttpGet]
        [Route("SearchVehicle")]
        public async Task<IActionResult> SearchVehicle(string searchTerm)
        {
            var result = await _service.SearchVehicleAsync(searchTerm);
            return Json(result);
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            // Kiểm tra quyền: chỉ Consulter mới được tạo check-in
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để truy cập trang này.";
                return RedirectToAction("Login", "Auth");
            }

            var roleIdInt = int.Parse(roleId);
            if (roleIdInt != 6) // Chỉ Consulter (RoleId = 6) mới được tạo check-in
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này. Chỉ Consulter mới được phép tạo check-in.";
                return RedirectToAction("Index", "VehicleCheckins");
            }

			return View("~/views/VehicleCheckins/Create.cshtml");
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            // Kiểm tra quyền: chỉ Consulter mới được sửa check-in
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để truy cập trang này.";
                return RedirectToAction("Login", "Auth");
            }

            var roleIdInt = int.Parse(roleId);
            if (roleIdInt != 6) // Chỉ Consulter (RoleId = 6) mới được sửa check-in
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này. Chỉ Consulter mới được phép sửa check-in.";
                return RedirectToAction("Index", "VehicleCheckins");
            }

            ViewBag.CheckinId = id;
			return View("~/views/VehicleCheckins/Edit.cshtml");
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            ViewBag.CheckinId = id;
			return View("~/views/VehicleCheckins/Details.cshtml");
        }

        [HttpGet]
        [Route("GetDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var data = await _service.GetByIdAsync(id);
            return Json(data);
        }

        [HttpPost]
        [Route("Update/{id}")]
        public async Task<IActionResult> Update(int id)
        {
            // Kiểm tra quyền: chỉ Consulter mới được cập nhật check-in
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện thao tác này." });
            }

            var roleIdInt = int.Parse(roleId);
            if (roleIdInt != 6) // Chỉ Consulter (RoleId = 6) mới được cập nhật check-in
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này. Chỉ Consulter mới được phép cập nhật check-in." });
            }

            try
            {
                // Lấy dữ liệu thật từ form
                var formData = new Dictionary<string, object>();
                foreach (var key in Request.Form.Keys)
                {
                    var value = Request.Form[key].ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        formData[key] = value;
                    }
                }
                
                Console.WriteLine($"Updating checkin {id} with real data:");
                foreach (var kv in formData)
                {
                    Console.WriteLine($"  {kv.Key}: {kv.Value}");
                }
                
                var result = await _service.UpdateAsync(id, formData);
                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Update: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
