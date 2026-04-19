using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BE.DTOs.TotalReceipt;
using BE.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TotalReceiptController : ControllerBase
    {
        private readonly ITotalReceiptService _service;
        private readonly IReportService _reportService;

        public TotalReceiptController(ITotalReceiptService service, IReportService reportService)
        {
            _service = service;
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null,
                                                  [FromQuery] string? statusCode = null, [FromQuery] DateTime? fromDate = null,
                                                  [FromQuery] DateTime? toDate = null, [FromQuery] long? branchId = null)
        {
            try
            {
                // ✅ Lấy userId và role từ JWT token
                long? userId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
                var roleIdClaim = User.FindFirst("RoleId")?.Value;
                bool isAdmin = string.Equals(roleName, "ADMIN", StringComparison.OrdinalIgnoreCase) || roleIdClaim == "1";

                // ✅ Nếu không phải Admin, luôn giới hạn theo BranchId trong token
                // Admin: chỉ áp branchId từ token khi client không truyền branchId (mặc định xem chi nhánh của mình)
                if (!branchId.HasValue || !isAdmin)
                {
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (long.TryParse(branchIdClaim, out var claimBranchId))
                    {
                        branchId = claimBranchId;
                    }
                }

                // ✅ Nếu là Admin và không truyền branchId => cho phép xem tất cả chi nhánh (không ép theo user branch)
                var effectiveUserId = isAdmin && !branchId.HasValue ? (long?)null : userId;

                var result = await _service.GetPagedAsync(page, pageSize, search, statusCode, fromDate, toDate, branchId, effectiveUserId);
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Invoice not found" });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("by-ticket/{maintenanceTicketId}")]
        public async Task<IActionResult> GetByMaintenanceTicket(long maintenanceTicketId)
        {
            try
            {
                var result = await _service.GetByMaintenanceTicketIdAsync(maintenanceTicketId);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Invoice not found" });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return Ok(new { success = true, data = created, message = "Invoice created successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] RequestDto dto)
        {
            try
            {
                // ✅ Lấy current user từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                long? currentUserId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var userId))
                {
                    currentUserId = userId;
                }

                var updated = await _service.UpdateAsync(id, dto, currentUserId);
                if (updated == null)
                {
                    return NotFound(new { success = false, message = "Invoice not found" });
                }

                return Ok(new { success = true, data = updated, message = "Invoice updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(new { success = false, message = "Invoice not found" });
                }

                return Ok(new { success = true, message = "Invoice deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Export PDF hóa đơn dịch vụ
        /// </summary>
        [HttpGet("{id}/export-pdf")]
        public async Task<IActionResult> ExportPdf(long id)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateTotalReceiptPdfAsync(id);
                var receipt = await _service.GetByIdAsync(id);
                var fileName = $"HoaDon_{receipt?.Code ?? id.ToString()}_{DateTime.Now:yyyyMMdd}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi để debug
                Console.WriteLine($"Error generating PDF for receipt {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
