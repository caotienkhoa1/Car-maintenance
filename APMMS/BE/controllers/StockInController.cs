using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BE.interfaces;
using BE.DTOs.StockIn;
using OfficeOpenXml;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockInController : ControllerBase
    {
        private readonly IStockInService _service;

        public StockInController(IStockInService service)
        {
            _service = service;
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

        [HttpGet("template")]
        public ActionResult DownloadTemplate()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Phiếu nhập kho");

                // Mã yêu cầu (row 1) - Format theo hình ảnh người dùng
                worksheet.Cells[1, 1].Value = "Mã yêu cầu: YC2501150001"; // Example - người dùng sẽ thay đổi
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 12;

                // Column headers (row 2) - Format theo hình ảnh người dùng
                worksheet.Cells[2, 1].Value = "STT";
                worksheet.Cells[2, 2].Value = "Mã linh kiện";
                worksheet.Cells[2, 3].Value = "Tên linh kiện";
                worksheet.Cells[2, 4].Value = "Loại";
                worksheet.Cells[2, 5].Value = "Số lượng yêu cầu";
                worksheet.Cells[2, 6].Value = "Số lượng thực tế";
                worksheet.Cells[2, 7].Value = "Giá nhập";
                worksheet.Cells[2, 8].Value = "Giá xuất";

                // Style header row
                using (var range = worksheet.Cells[2, 1, 2, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // Không có dữ liệu mẫu - người dùng sẽ điền dữ liệu thực tế từ dòng 3

                // Auto fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Mau_Phieu_Nhap_Kho_{DateTime.Now:yyyyMMdd}.xlsx";
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

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] StockInRequestDto dto)
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

                var created = await _service.CreateFromRequestAsync(dto, userId);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, new { success = true, data = created, message = "Phiếu nhập kho đã được tạo thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Xử lý lỗi database constraint
                var innerException = ex.InnerException?.Message ?? ex.Message;
                string errorMessage = "Lỗi khi lưu dữ liệu vào database";
                
                if (innerException.Contains("FOREIGN KEY") || innerException.Contains("foreign key"))
                {
                    errorMessage = "Yêu cầu nhập kho không tồn tại hoặc không hợp lệ. Vui lòng kiểm tra mã yêu cầu: " + (dto.StockInRequestCode ?? "");
                }
                else if (innerException.Contains("UNIQUE") || innerException.Contains("unique"))
                {
                    errorMessage = "Dữ liệu đã tồn tại trong hệ thống";
                }
                else if (innerException.Contains("NOT NULL") || innerException.Contains("not null"))
                {
                    errorMessage = "Thiếu thông tin bắt buộc";
                }
                
                return BadRequest(new { success = false, message = errorMessage, error = innerException });
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                var innerException = ex.InnerException?.Message ?? "";
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message,
                    error = innerException
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(long id, [FromBody] StockInRequestDto dto)
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

                dto.Id = id;
                var updated = await _service.UpdateAsync(dto, userId);
                return Ok(new { success = true, data = updated, message = "Phiếu nhập kho đã được cập nhật thành công" });
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
        public async Task<ActionResult> Approve(long id, [FromBody] StockInRequestDto dto)
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

                dto.Id = id;
                var updated = await _service.ApproveAsync(dto, userId);
                return Ok(new { success = true, data = updated, message = "Phiếu nhập kho đã được duyệt và cập nhật kho thành công" });
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

        [HttpPut("{id}/quantity-after-check")]
        public async Task<ActionResult> UpdateQuantityAfterCheck(long id, [FromBody] StockInRequestDto dto)
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

                dto.Id = id;
                var updated = await _service.UpdateQuantityAfterCheckAsync(dto, userId);
                return Ok(new { success = true, data = updated, message = "Số lượng sau kiểm tra đã được cập nhật thành công" });
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
        public async Task<ActionResult> Cancel(long id)
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

                var updated = await _service.CancelAsync(id, userId);
                return Ok(new { success = true, data = updated, message = "Phiếu nhập kho đã được hủy thành công" });
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
    }
}

