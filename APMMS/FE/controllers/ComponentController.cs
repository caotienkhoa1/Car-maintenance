using FE.services;
using Microsoft.AspNetCore.Mvc;

namespace FE.controllers
{
    [Route("Components")]
    public class ComponentController : Controller
    {
        private readonly ComponentService _service;

        public ComponentController(ComponentService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            return View("~/views/Components/Index.cshtml");
        }

        [HttpGet]
        [Route("ListData")]
        public async Task<IActionResult> ListData(int page = 1, int pageSize = 10, string? search = null, string? statusCode = null)
        {
            // BE sẽ tự động lấy branchId từ JWT token, không cần FE gửi lên
            var data = await _service.GetAllAsync(page, pageSize, search, statusCode);
            return Json(data);
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            return View("~/views/Components/Create.cshtml");
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            ViewBag.ComponentId = id;
            return View("~/views/Components/Edit.cshtml");
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            ViewBag.ComponentId = id;
            return View("~/views/Components/Details.cshtml");
        }

        [HttpGet]
        [Route("GetDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var data = await _service.GetByIdAsync(id);
            return Json(data);
        }

        [HttpPost]
        [Route("ToggleStatus")]
        public async Task<IActionResult> ToggleStatus(long id, string statusCode)
        {
            try
            {
                var ok = await _service.ToggleStatusAsync(id, statusCode);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Update/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] object data)
        {
            try
            {
                var result = await _service.UpdateAsync(id, data);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
