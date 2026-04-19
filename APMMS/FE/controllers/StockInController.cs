using FE.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace FE.controllers
{
    [Route("StockIns")]
    public class StockInController : Controller
    {
        private readonly StockInService _service;
        private readonly IConfiguration _configuration;

        public StockInController(StockInService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            return View("~/views/StockIns/Index.cshtml");
        }

        [HttpGet]
        [Route("ListData")]
        public async Task<IActionResult> ListData(int page = 1, int pageSize = 10, string? search = null, string? statusCode = null)
        {
            // Lấy branchId của user đang đăng nhập
            long? branchId = null;

            var branchIdFromSession = HttpContext.Session.GetString("BranchId");
            if (!string.IsNullOrEmpty(branchIdFromSession) && long.TryParse(branchIdFromSession, out var sessionBranchId))
            {
                branchId = sessionBranchId;
            }
            else
            {
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                if (long.TryParse(branchIdClaim, out var claimBranchId))
                {
                    branchId = claimBranchId;
                }
            }

            var result = await _service.GetAllAsync(page, pageSize, search, statusCode, branchId);
            return Json(result);
        }

        [HttpGet]
        [Route("Template")]
        public async Task<IActionResult> DownloadTemplate()
        {
            try
            {
                var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
                var httpClient = new HttpClient();
                var token = HttpContext.Session.GetString("AuthToken") 
                    ?? HttpContext.Request.Cookies["authToken"];
                
                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/StockIn/template");
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    var contentDisposition = response.Content.Headers.ContentDisposition;
                    var fileName = contentDisposition?.FileName?.Trim('"') 
                        ?? $"Mau_Phieu_Nhap_Kho_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                return BadRequest("Không thể tải file mẫu");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Details(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }

            ViewBag.StockIn = result;
            return View("~/views/StockIns/Details.cshtml");
        }

        [HttpGet]
        [Route("{id}/Data")]
        public async Task<IActionResult> GetData(long id)
        {
            try
            {
                // Gọi trực tiếp backend API thay vì qua service để tránh double wrapping
                var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
                var httpClient = new HttpClient();
                var token = HttpContext.Session.GetString("AuthToken") 
                    ?? HttpContext.Request.Cookies["authToken"];
                
                var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/StockIn/{id}");
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, Content(errorContent, "application/json"));
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn file Excel" });
            }

            var result = await _service.UploadAsync(file);
            return Json(result);
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create([FromBody] object data)
        {
            var result = await _service.CreateAsync(data);
            return Json(result);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] object data)
        {
            var result = await _service.UpdateAsync(id, data);
            return Json(result);
        }

        [HttpPost]
        [Route("{id}/Approve")]
        public async Task<IActionResult> Approve(long id, [FromBody] object data)
        {
            var result = await _service.ApproveAsync(id, data);
            return Json(result);
        }

        [HttpPut]
        [Route("{id}/QuantityAfterCheck")]
        public async Task<IActionResult> UpdateQuantityAfterCheck(long id, [FromBody] object data)
        {
            var result = await _service.UpdateQuantityAfterCheckAsync(id, data);
            return Json(result);
        }

        [HttpPost]
        [Route("{id}/Cancel")]
        public async Task<IActionResult> Cancel(long id)
        {
            var result = await _service.CancelAsync(id);
            return Json(result);
        }

        [HttpPatch]
        [Route("{id}/Status")]
        public async Task<IActionResult> ChangeStatus(long id, [FromQuery] string statusCode)
        {
            var result = await _service.ChangeStatusAsync(id, statusCode);
            return Json(result);
        }
    }
}

