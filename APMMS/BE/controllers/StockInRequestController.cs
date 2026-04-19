using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BE.interfaces;
using BE.DTOs.StockInRequest;
using BE.repository.IRepository;
using OfficeOpenXml;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockInRequestController : ControllerBase
    {
        private readonly IStockInRequestService _service;
        private readonly IUserRepository _userRepository;

        public StockInRequestController(IStockInRequestService service, IUserRepository userRepository)
        {
            _service = service;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] long? branchId = null,
            [FromQuery] string? statusCode = null,
            [FromQuery] string? search = null)
        {
            try
            {
                // Lấy userId từ JWT token
                long? userId = null;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                // Lấy BranchId từ JWT claim
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                if (long.TryParse(branchIdClaim, out var claimBranchId))
                {
                    branchId = claimBranchId;
                }

                var result = await _service.GetAllAsync(page, pageSize, branchId, statusCode, search);
                var totalCount = await _service.GetTotalCountAsync(branchId, statusCode, search);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return Ok(new
                {
                    success = true,
                    data = result,
                    page = page,
                    pageSize = pageSize,
                    totalPages = totalPages,
                    currentPage = page,
                    totalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> Get(long id)
        {
            try
            {
                var item = await _service.GetByIdAsync(id);
                return Ok(new { success = true, data = item });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] StockInRequestRequestDto dto)
        {
            try
            {
                // Lấy userId từ JWT token
                long userId = 0;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[StockInRequestController.Create] UserId from JWT claim: {userIdClaim}");
                
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                    Console.WriteLine($"[StockInRequestController.Create] Parsed userId: {userId}");
                }
                else
                {
                    Console.WriteLine($"[StockInRequestController.Create] ❌ Cannot parse userId from claim: {userIdClaim}");
                    // Log all claims for debugging
                    Console.WriteLine($"[StockInRequestController.Create] All claims:");
                    foreach (var claim in User.Claims)
                    {
                        Console.WriteLine($"[StockInRequestController.Create]   {claim.Type} = {claim.Value}");
                    }
                }

                // Validate userId exists
                if (userId == 0)
                {
                    Console.WriteLine($"[StockInRequestController.Create] ❌ ERROR: userId is 0 or invalid");
                    return BadRequest(new { success = false, message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
                }

                // Verify user exists in database
                var userExists = await _userRepository.GetByIdAsync(userId);
                if (userExists == null)
                {
                    Console.WriteLine($"[StockInRequestController.Create] ❌ ERROR: User with Id {userId} does not exist in database");
                    return BadRequest(new { success = false, message = $"Người dùng với ID {userId} không tồn tại trong hệ thống." });
                }

                Console.WriteLine($"[StockInRequestController.Create] ✅ User validated: Id={userExists.Id}, Username={userExists.Username}");

                long? branchId = null;

                // Ưu tiên 1: Lấy BranchId từ JWT claim
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                Console.WriteLine($"[StockInRequestController.Create] BranchId from JWT claim: {branchIdClaim}");

                if (!string.IsNullOrEmpty(branchIdClaim) && long.TryParse(branchIdClaim, out var claimBranchId))
                {
                    branchId = claimBranchId;
                    Console.WriteLine($"[StockInRequestController.Create] Using branchId from JWT: {branchId}");
                }
                else if (userId > 0)
                {
                    // Ưu tiên 2: Lấy BranchId từ User table
                    try
                    {
                        var user = await _userRepository.GetByIdAsync(userId);
                        if (user != null)
                        {
                            Console.WriteLine($"[StockInRequestController.Create] User found: Id={user.Id}, Username={user.Username}, BranchId={user.BranchId}");

                            if (user.BranchId.HasValue)
                            {
                                branchId = user.BranchId;
                                Console.WriteLine($"[StockInRequestController.Create] ✅ Got branchId from user profile: {branchId}");
                            }
                            else
                            {
                                Console.WriteLine($"[StockInRequestController.Create] ❌ User {userId} ({user.Username}) does not have BranchId assigned");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[StockInRequestController.Create] ❌ Không tìm thấy người dùng trong database, Id: {userId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StockInRequestController.Create] ❌ Error querying user from database: {ex.Message}");
                    }
                }

                // Ưu tiên 3: Fallback to DTO (nhưng không nên tin tưởng)
                if (!branchId.HasValue && dto.BranchId > 0)
                {
                    branchId = dto.BranchId;
                    Console.WriteLine($"[StockInRequestController.Create] ⚠️ Using branchId from request body (fallback): {branchId}");
                }

                if (!branchId.HasValue)
                {
                    Console.WriteLine($"[StockInRequestController.Create] ❌ ERROR: Cannot determine branchId after all attempts");
                    Console.WriteLine($"[StockInRequestController.Create] UserId: {userId}, Dto.BranchId: {dto.BranchId}");
                    return BadRequest(new { success = false, message = "Không thể xác định chi nhánh của người dùng. Vui lòng liên hệ quản trị viên để được gán vào một chi nhánh trước khi tạo yêu cầu nhập kho." });
                }

                // LUÔN set BranchId từ User/JWT (không tin tưởng vào DTO từ frontend)
                dto.BranchId = branchId.Value;
                Console.WriteLine($"[StockInRequestController.Create] ✅ Creating StockInRequest with BranchId: {branchId}, UserId: {userId}");

                var created = await _service.CreateAsync(dto, userId);
                return CreatedAtAction(nameof(Get), new { id = created.StockInRequestId }, new { success = true, data = created, message = "Yêu cầu nhập kho đã được tạo thành công" });
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"[StockInRequestController.Create] ArgumentException: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StockInRequestController.Create] Exception: {ex.Message}");
                Console.WriteLine($"[StockInRequestController.Create] InnerException: {ex.InnerException?.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(long id, [FromBody] StockInRequestRequestDto dto)
        {
            try
            {
                // Lấy userId từ JWT token
                long userId = 0;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                dto.StockInRequestId = id;
                var updated = await _service.UpdateAsync(dto, userId);
                return Ok(new { success = true, data = updated, message = "Yêu cầu nhập kho đã được cập nhật thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult> ChangeStatus(long id, [FromQuery] string statusCode)
        {
            try
            {
                // Lấy userId từ JWT token
                long userId = 0;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var updated = await _service.ChangeStatusAsync(id, statusCode, userId);
                return Ok(new { success = true, data = updated, message = "Trạng thái đã được cập nhật thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<ActionResult> Approve(long id)
        {
            try
            {
                // Kiểm tra role: chỉ Branch Manager (RoleId = 2) mới được duyệt
                var roleIdClaim = User.FindFirst("RoleId")?.Value;
                var roleId = long.TryParse(roleIdClaim, out var parsedRoleId) ? parsedRoleId : 0;
                
                if (roleId != 2) // 2 = Branch Manager
                {
                    return StatusCode(403, new { success = false, message = "Chỉ giám đốc chi nhánh mới được duyệt yêu cầu nhập kho" });
                }

                // Lấy userId từ JWT token
                long userId = 0;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var updated = await _service.ApproveAsync(id, userId);
                return Ok(new { success = true, data = updated, message = "Yêu cầu nhập kho đã được duyệt thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("{id}/send")]
        public async Task<ActionResult> Send(long id)
        {
            try
            {
                // Kiểm tra role: chỉ Warehouse Keeper (RoleId = 5) mới được gửi đơn
                var roleIdClaim = User.FindFirst("RoleId")?.Value;
                var roleId = long.TryParse(roleIdClaim, out var parsedRoleId) ? parsedRoleId : 0;
                
                if (roleId != 5) // 5 = Warehouse Keeper
                {
                    return StatusCode(403, new { success = false, message = "Chỉ quản lý kho mới được gửi yêu cầu nhập kho" });
                }

                // Lấy userId từ JWT token
                long userId = 0;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var updated = await _service.ChangeStatusAsync(id, "PENDING", userId);
                return Ok(new { success = true, data = updated, message = "Gửi yêu cầu nhập kho thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> Cancel(long id, [FromBody] CancelRequestDto? cancelDto = null)
        {
            try
            {
                // Kiểm tra role: chỉ Branch Manager (RoleId = 2) mới được hủy
                var roleIdClaim = User.FindFirst("RoleId")?.Value;
                var roleId = long.TryParse(roleIdClaim, out var parsedRoleId) ? parsedRoleId : 0;
                
                if (roleId != 2) // 2 = Branch Manager
                {
                    return StatusCode(403, new { success = false, message = "Chỉ giám đốc chi nhánh mới được hủy yêu cầu nhập kho" });
                }

                // Lấy userId từ JWT token
                long userId = 0;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var updated = await _service.CancelAsync(id, cancelDto?.Note, userId);
                return Ok(new { success = true, data = updated, message = "Yêu cầu nhập kho đã được hủy thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("status/{statusCode}")]
        public async Task<ActionResult> GetByStatus(string statusCode)
        {
            try
            {
                var result = await _service.GetByStatusAsync(statusCode);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("template")]
        public ActionResult DownloadTemplate()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Yeu_cau_nhap_kho");

                // Header row
                worksheet.Cells[1, 1].Value = "Mã linh kiện";
                worksheet.Cells[1, 2].Value = "Tên linh kiện";
                worksheet.Cells[1, 3].Value = "Loại";
                worksheet.Cells[1, 4].Value = "Số lượng";

                // Style header row
                using (var range = worksheet.Cells[1, 1, 1, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // Sample data row (example)
                worksheet.Cells[2, 1].Value = "LK001";
                worksheet.Cells[2, 2].Value = "Lọc dầu động cơ";
                worksheet.Cells[2, 3].Value = "Phụ tùng";
                worksheet.Cells[2, 4].Value = 10;

                // Auto fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Mau_Yeu_Cau_Nhap_Kho_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(stream.ToArray(), 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi tạo file mẫu", error = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<ActionResult> UploadExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Vui lòng chọn file Excel" });
                }

                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return BadRequest(new { success = false, message = "File phải là định dạng Excel (.xlsx hoặc .xls)" });
                }

                var result = await _service.UploadExcelAsync(file);
                return Ok(new { success = true, data = result, message = "Đọc file Excel thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }

    public class CancelRequestDto
    {
        public string? Note { get; set; }
    }
}

