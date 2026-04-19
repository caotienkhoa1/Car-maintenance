using FE.services;
using Microsoft.AspNetCore.Mvc;

namespace FE.controllers
{
    [Route("TypeComponents")]
    public class TypeComponentController : Controller
    {
        private readonly TypeComponentService _service;

        public TypeComponentController(TypeComponentService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            return View("~/views/TypeComponents/Index.cshtml");
        }

        [HttpGet]
        [Route("ListData")]
        public async Task<IActionResult> ListData(int page = 1, int pageSize = 10, string? search = null, string? statusCode = null, long? branchId = null)
        {
            // ✅ Nhận branchId từ query parameter và truyền xuống service
            // Admin có thể filter theo chi nhánh, user thường sẽ bị filter tự động bởi BE
            var data = await _service.GetAllAsync(page, pageSize, search, statusCode, branchId);
            return Json(data);
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            return View("~/views/TypeComponents/Create.cshtml");
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create([FromBody] object data)
        {
            try
            {
                var result = await _service.CreateAsync(data);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            ViewBag.TypeComponentId = id;
            return View("~/views/TypeComponents/Edit.cshtml");
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            ViewBag.TypeComponentId = id;
            return View("~/views/TypeComponents/Details.cshtml");
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
