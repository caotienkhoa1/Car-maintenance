using FE.services;
using Microsoft.AspNetCore.Mvc;

namespace FE.controllers
{
    [Route("StockInRequests")]
    public class StockInRequestController : Controller
    {
        private readonly StockInRequestService _service;

        public StockInRequestController(StockInRequestService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            return View("~/views/StockInRequests/Index.cshtml");
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
        [Route("Create")]
        public IActionResult Create()
        {
            return View("~/views/StockInRequests/Create.cshtml");
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create([FromBody] object data)
        {
            // Lấy BranchId từ session hoặc JWT claim
            long? branchId = null;
            var branchIdFromSession = HttpContext.Session.GetString("BranchId");
            Console.WriteLine($"[FE StockInRequestController] BranchId from session: {branchIdFromSession}");

            if (!string.IsNullOrEmpty(branchIdFromSession) && long.TryParse(branchIdFromSession, out var sessionBranchId))
            {
                branchId = sessionBranchId;
                Console.WriteLine($"[FE StockInRequestController] ✅ Got branchId from session: {branchId}");
            }
            else
            {
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                Console.WriteLine($"[FE StockInRequestController] BranchId from JWT claim: {branchIdClaim}");
                if (long.TryParse(branchIdClaim, out var claimBranchId))
                {
                    branchId = claimBranchId;
                    HttpContext.Session.SetString("BranchId", branchId.Value.ToString());
                    Console.WriteLine($"[FE StockInRequestController] ✅ Got branchId from JWT claim: {branchId}");
                }
            }

            if (!branchId.HasValue)
            {
                Console.WriteLine("[FE StockInRequestController] ❌ ERROR: Cannot determine branchId after all attempts");
                return Json(new
                {
                    success = false,
                    message = "Không thể xác định chi nhánh của người dùng. Vui lòng đăng nhập lại."
                });
            }

            Console.WriteLine($"[FE StockInRequestController] ✅ Final branchId: {branchId}");

            // Convert data to dictionary and add BranchId
            System.Collections.Generic.Dictionary<string, object> dataDict;

            if (data is System.Text.Json.JsonElement dataJsonElement)
            {
                dataDict = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(
                    dataJsonElement.GetRawText(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new System.Collections.Generic.Dictionary<string, object>();
            }
            else if (data is System.Collections.Generic.IDictionary<string, object> dict)
            {
                dataDict = new System.Collections.Generic.Dictionary<string, object>(dict);
            }
            else
            {
                dataDict = new System.Collections.Generic.Dictionary<string, object>();
                var dataType = data.GetType();
                foreach (var prop in dataType.GetProperties())
                {
                    var value = prop.GetValue(data);
                    dataDict[prop.Name] = value ?? (object)string.Empty;
                }
            }

            // Luôn set BranchId từ session/JWT (không tin tưởng vào frontend)
            dataDict["branchId"] = branchId.Value;
            Console.WriteLine($"[FE StockInRequestController] Added branchId to data: {branchId.Value}");

            var result = await _service.CreateAsync(dataDict);
            return Json(result);
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

            ViewBag.StockInRequest = result;
            return View("~/views/StockInRequests/Details.cshtml");
        }

        [HttpGet]
        [Route("{id}/Data")]
        public async Task<IActionResult> GetData(long id)
        {
            var result = await _service.GetByIdAsync(id);
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
        public async Task<IActionResult> Approve(long id)
        {
            var result = await _service.ApproveAsync(id);
            return Json(result);
        }

        [HttpPost]
        [Route("{id}/Send")]
        public async Task<IActionResult> Send(long id)
        {
            var result = await _service.SendAsync(id);
            return Json(result);
        }

        [HttpPost]
        [Route("{id}/Cancel")]
        public async Task<IActionResult> Cancel(long id, [FromBody] object? data = null)
        {
            string? note = null;
            if (data != null)
            {
                var noteProperty = data.GetType().GetProperty("note") ?? data.GetType().GetProperty("Note");
                if (noteProperty != null)
                {
                    note = noteProperty.GetValue(data)?.ToString();
                }
            }

            var result = await _service.CancelAsync(id, note);
            return Json(result);
        }

        [HttpPatch]
        [Route("{id}/Status")]
        public async Task<IActionResult> ChangeStatus(long id, [FromQuery] string statusCode)
        {
            var result = await _service.ChangeStatusAsync(id, statusCode);
            return Json(result);
        }

        [HttpGet]
        [Route("Template")]
        public async Task<IActionResult> DownloadTemplate()
        {
            var result = await _service.DownloadTemplateAsync();
            if (result.Success && result.Data != null)
            {
                return File(result.Data, result.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName ?? $"Mau_Yeu_Cau_Nhap_Kho_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            return BadRequest(new { success = false, message = result.Message ?? "Không thể tải file mẫu" });
        }

        [HttpPost]
        [Route("Upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn file Excel" });
            }

            var result = await _service.UploadExcelAsync(file);
            return Json(result);
        }
    }
}

