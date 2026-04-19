using AutoMapper;
using BE.DTOs.MaintenanceTicket;
using BE.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MaintenanceTicketController : ControllerBase
    {
        private readonly IMaintenanceTicketService _maintenanceTicketService;
        private readonly IMapper _mapper;
        private readonly IReportService _reportService;

        public MaintenanceTicketController(
            IMaintenanceTicketService maintenanceTicketService, 
            IMapper mapper,
            IReportService reportService)
        {
            _maintenanceTicketService = maintenanceTicketService;
            _mapper = mapper;
            _reportService = reportService;
        }

        /// <summary>
        /// T?o Maintenance Ticket m?i
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateMaintenanceTicket([FromBody] RequestDto request)
        {
            try
            {
                var result = await _maintenanceTicketService.CreateMaintenanceTicketAsync(request);
                return Ok(new { success = true, data = result, message = "Maintenance ticket created successfully" });
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

        /// <summary>
        /// T?o Maintenance Ticket t? Vehicle Check-in
        /// </summary>
        [HttpPost("create-from-checkin")]
        public async Task<IActionResult> CreateFromVehicleCheckin([FromBody] CreateFromCheckinDto request)
        {
            try
            {
                var result = await _maintenanceTicketService.CreateFromVehicleCheckinAsync(request);
                return Ok(new { success = true, data = result, message = "Maintenance ticket created from vehicle check-in successfully" });
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

        /// <summary>
        /// C?p nh?t Maintenance Ticket
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaintenanceTicket(long id, [FromBody] RequestDto request)
        {
            try
            {
                var result = await _maintenanceTicketService.UpdateMaintenanceTicketAsync(id, request);
                return Ok(new { success = true, data = result, message = "Maintenance ticket updated successfully" });
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

        /// <summary>
        /// Lấy lịch sử hoạt động của Maintenance Ticket
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistoryLogs(long id)
        {
            try
            {
                var result = await _maintenanceTicketService.GetHistoryLogsAsync(id);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// L?y Maintenance Ticket theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMaintenanceTicketById(long id)
        {
            try
            {
                var result = await _maintenanceTicketService.GetMaintenanceTicketByIdAsync(id);
                return Ok(new { success = true, data = result });
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

        /// <summary>
        /// L?y danh s�ch t?t c? Maintenance Tickets
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllMaintenanceTickets([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] long? branchId = null)
        {
            try
            {
                // ✅ Nếu có BranchId trong JWT claim, ưu tiên dùng nó
                if (!branchId.HasValue && User.Identity?.IsAuthenticated == true)
                {
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (long.TryParse(branchIdClaim, out var claimBranchId))
                    {
                        branchId = claimBranchId;
                    }
                }
                
                var result = await _maintenanceTicketService.GetAllMaintenanceTicketsAsync(page, pageSize, branchId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// L?y Maintenance Tickets theo Car ID
        /// </summary>
        [HttpGet("by-car/{carId}")]
        public async Task<IActionResult> GetMaintenanceTicketsByCarId(long carId)
        {
            try
            {
                var result = await _maintenanceTicketService.GetMaintenanceTicketsByCarIdAsync(carId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("customer/history")]
        [Authorize]
        public async Task<IActionResult> GetMaintenanceHistoryForCustomer([FromQuery] long? userId = null)
        {
            try
            {
                long? resolvedUserId = userId;
                var claimUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!resolvedUserId.HasValue && long.TryParse(claimUserIdValue, out var claimUserId))
                {
                    resolvedUserId = claimUserId;
                }

                if (!resolvedUserId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Không xác định được người dùng." });
                }

                // ✅ Cho phép Admin, Branch Manager, và Consultant xem lịch sử của khách hàng
                // Consultant có thể xem lịch sử của khách hàng ở bất kỳ chi nhánh nào
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Branch Manager");
                var isConsultant = User.IsInRole("Consultant");
                
                if (!isAdmin && !isConsultant && claimUserIdValue != null && long.TryParse(claimUserIdValue, out var loggedInUserId))
                {
                    // Chỉ user thường mới bị giới hạn xem lịch sử của chính họ
                    if (loggedInUserId != resolvedUserId.Value)
                    {
                        return Forbid("Bạn không được phép xem lịch sử của người dùng khác.");
                    }
                }

                // ✅ Load lịch sử từ TẤT CẢ chi nhánh, không filter theo branchId của user đang đăng nhập
                var result = await _maintenanceTicketService.GetMaintenanceHistoryByUserIdAsync(resolvedUserId.Value);
                
                System.Diagnostics.Debug.WriteLine($"[GetMaintenanceHistoryForCustomer] User {claimUserIdValue} (Admin: {isAdmin}, Consultant: {isConsultant}) viewing history of userId: {resolvedUserId.Value}, returned {result.Count} items");
                
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// L?y Maintenance Tickets theo Status
        /// </summary>
        [HttpGet("by-status/{statusCode}")]
        public async Task<IActionResult> GetMaintenanceTicketsByStatus(string statusCode)
        {
            try
            {
                var result = await _maintenanceTicketService.GetMaintenanceTicketsByStatusAsync(statusCode);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// C?p nh?t Status c?a Maintenance Ticket
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusDto request)
        {
            try
            {
                var result = await _maintenanceTicketService.UpdateStatusAsync(id, request.StatusCode);
                return Ok(new { success = true, data = result, message = "Status updated successfully" });
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

        /// <summary>
        /// X�a Maintenance Ticket
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaintenanceTicket(long id)
        {
            try
            {
                var result = await _maintenanceTicketService.DeleteMaintenanceTicketAsync(id);
                if (result)
                {
                    return Ok(new { success = true, message = "Maintenance ticket deleted successfully" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Maintenance ticket not found" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Gán kỹ thuật viên cho Maintenance Ticket
        /// </summary>
        [HttpPut("{id}/assign-technician")]
        public async Task<IActionResult> AssignTechnician(long id, [FromBody] AssignTechnicianDto request)
        {
            try
            {
                var result = await _maintenanceTicketService.AssignTechnicianAsync(id, request.TechnicianId);
                return Ok(new { success = true, data = result, message = "Technician assigned successfully" });
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

        /// <summary>
        /// Thêm nhiều kỹ thuật viên cho Maintenance Ticket
        /// </summary>
        [HttpPut("{id}/technicians")]
        public async Task<IActionResult> AddTechnicians(long id, [FromBody] AssignTechniciansDto request)
        {
            try
            {
                var result = await _maintenanceTicketService.AddTechniciansAsync(id, request?.TechnicianIds ?? new List<long>(), request?.PrimaryId);
                return Ok(new { success = true, data = result, message = "Technicians added successfully" });
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

        /// <summary>
        /// Xóa tất cả kỹ thuật viên khỏi Maintenance Ticket
        /// </summary>
        [HttpDelete("{id}/technicians")]
        public async Task<IActionResult> RemoveTechnicians(long id)
        {
            try
            {
                var result = await _maintenanceTicketService.RemoveTechniciansAsync(id);
                return Ok(new { success = true, data = result, message = "Technicians removed successfully" });
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

        /// <summary>
        /// Bắt đầu bảo dưỡng
        /// </summary>
        [HttpPut("{id}/start")]
        public async Task<IActionResult> StartMaintenance(long id)
        {
            try
            {
                var result = await _maintenanceTicketService.StartMaintenanceAsync(id);
                return Ok(new { success = true, data = result, message = "Maintenance started successfully" });
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

        /// <summary>
        /// Hoàn thành bảo dưỡng
        /// </summary>
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteMaintenance(long id)
        {
            try
            {
                // Lấy userId từ claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                long? userId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;
                
                var result = await _maintenanceTicketService.CompleteMaintenanceAsync(id, userId);
                return Ok(new { success = true, data = result, message = "Maintenance completed successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
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

        /// <summary>
        /// Hủy phiếu bảo dưỡng (status = CANCELLED)
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelMaintenanceTicket(long id)
        {
            try
            {
                var result = await _maintenanceTicketService.CancelMaintenanceTicketAsync(id);
                return Ok(new { success = true, data = result, message = "Phiếu bảo dưỡng đã được hủy thành công" });
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

        /// <summary>
        /// Áp dụng Service Package vào Maintenance Ticket - tự động thêm các Components từ package
        /// </summary>
        [HttpPut("{id}/apply-service-package/{servicePackageId}")]
        public async Task<IActionResult> ApplyServicePackage(long id, long servicePackageId)
        {
            try
            {
                var result = await _maintenanceTicketService.ApplyServicePackageAsync(id, servicePackageId);
                return Ok(new { success = true, data = result, message = "Áp dụng gói dịch vụ thành công" });
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

        /// <summary>
        /// Xóa gói dịch vụ đã áp dụng khỏi Maintenance Ticket - xóa các Components từ package
        /// </summary>
        [HttpDelete("{id}/remove-service-package")]
        public async Task<IActionResult> RemoveServicePackage(long id)
        {
            try
            {
                var result = await _maintenanceTicketService.RemoveServicePackageAsync(id);
                return Ok(new { success = true, data = result, message = "Xóa gói dịch vụ thành công" });
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

        /// <summary>
        /// Export PDF báo cáo chi tiết phiếu bảo dưỡng
        /// </summary>
        [HttpGet("{id}/export-pdf")]
        public async Task<IActionResult> ExportPdf(long id)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateMaintenanceTicketPdfAsync(id);
                var ticket = await _maintenanceTicketService.GetMaintenanceTicketByIdAsync(id);
                var fileName = $"PhieuBaoDuong_{(ticket?.Code ?? id.ToString())}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
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

        /// <summary>
        /// Export PDF Báo giá (Quotation)
        /// </summary>
        [HttpGet("{id}/export-quotation")]
        public async Task<IActionResult> ExportQuotation(long id)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateQuotationPdfAsync(id);
                var ticket = await _maintenanceTicketService.GetMaintenanceTicketByIdAsync(id);
                var fileName = $"BaoGia_{(ticket?.Code ?? id.ToString())}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Export PDF Phiếu tạm tính (Provisional Invoice)
        /// </summary>
        [HttpGet("{id}/export-provisional")]
        public async Task<IActionResult> ExportProvisional(long id)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateProvisionalInvoicePdfAsync(id);
                var ticket = await _maintenanceTicketService.GetMaintenanceTicketByIdAsync(id);
                var fileName = $"TamTinh_{(ticket?.Code ?? id.ToString())}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
