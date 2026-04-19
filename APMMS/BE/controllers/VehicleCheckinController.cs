using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE.DTOs.VehicleCheckin;
using BE.interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace controllers
{
    /// <summary>
    /// Controller qu?n l� Vehicle Check-in
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleCheckinController : ControllerBase
    {
        private readonly IVehicleCheckinService _vehicleCheckinService;

        public VehicleCheckinController(IVehicleCheckinService vehicleCheckinService)
        {
            _vehicleCheckinService = vehicleCheckinService;
        }

        /// <summary>
        /// T?o m?i vehicle check-in
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Consulter")] // Chỉ Consulter được tạo check-in
        public async Task<IActionResult> CreateVehicleCheckin([FromBody] VehicleCheckinRequestDto request)
        {
            try
            {
                // Get current user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                long? createdByUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;
                
                var result = await _vehicleCheckinService.CreateVehicleCheckinAsync(request, createdByUserId);
                return Ok(new { success = true, data = result, message = "Vehicle check-in created successfully" });
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
        /// C?p nh?t vehicle check-in
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Consulter")] // Chỉ Consulter được sửa check-in
        public async Task<IActionResult> UpdateVehicleCheckin(long id, [FromBody] UpdateDto request)
        {
            try
            {
                if (id != request.Id)
                    return BadRequest(new { success = false, message = "ID mismatch" });

                var result = await _vehicleCheckinService.UpdateVehicleCheckinAsync(request);
                return Ok(new { success = true, data = result, message = "Vehicle check-in updated successfully" });
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
        /// L?y chi ti?t vehicle check-in theo ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Branch Manager,Consulter,Technician")] // Admin, Giám đốc, Consulter và KTV xem được
        public async Task<IActionResult> GetVehicleCheckinById(long id)
        {
            try
            {
                var result = await _vehicleCheckinService.GetVehicleCheckinByIdAsync(id);
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
        /// L?y danh s�ch t?t c? vehicle check-in (c� ph�n trang)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Branch Manager,Consulter,Technician")] // Cho phép KTV xem danh sách
        public async Task<IActionResult> GetAllVehicleCheckins(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? statusCode = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] long? branchId = null)
        {
            try
            {
                // ✅ Lấy userId từ JWT token để service tự động lấy BranchId
                long? userId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                    System.Diagnostics.Debug.WriteLine($"[VehicleCheckinController] Got userId from JWT: {userId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[VehicleCheckinController] No userId found in JWT token");
                }

                // Kiểm tra nếu user là Admin
                var roleIdClaim = User.FindFirst("RoleId")?.Value;
                var roleId = long.TryParse(roleIdClaim, out var parsedRoleId) ? parsedRoleId : 0;
                var isAdmin = roleId == 1;

                // Nếu là Admin và có branchId từ query parameter, dùng nó (cho phép Admin filter theo chi nhánh)
                // Nếu là Admin và không có branchId từ query, không filter (hiển thị tất cả) - giữ nguyên null
                if (!isAdmin)
                {
                    // Nếu không phải Admin, LUÔN lấy BranchId từ JWT claim
                    if (!branchId.HasValue)
                    {
                        var branchIdClaim = User.FindFirst("BranchId")?.Value;
                        if (long.TryParse(branchIdClaim, out var claimBranchId))
                        {
                            branchId = claimBranchId;
                            System.Diagnostics.Debug.WriteLine($"[VehicleCheckinController] Got branchId from JWT claim: {branchId}");
                        }
                    }
                }
                // Nếu là Admin: giữ nguyên branchId từ query (null = hiển thị tất cả, có giá trị = filter theo chi nhánh)

                System.Diagnostics.Debug.WriteLine($"[VehicleCheckinController] Final branchId: {branchId}, userId: {userId}, statusCode: {statusCode}");

                var result = await _vehicleCheckinService.GetAllVehicleCheckinsAsync(page, pageSize, searchTerm, statusCode, fromDate, toDate, userId, branchId);
                var totalCount = await _vehicleCheckinService.GetTotalCountAsync(searchTerm, statusCode, fromDate, toDate, userId, branchId);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                return Ok(new { 
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

        /// <summary>
        /// L?y danh s�ch vehicle check-in theo Car ID
        /// </summary>
        [HttpGet("car/{carId}")]
        public async Task<IActionResult> GetVehicleCheckinsByCarId(long carId)
        {
            try
            {
                var result = await _vehicleCheckinService.GetVehicleCheckinsByCarIdAsync(carId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// L?y danh s�ch vehicle check-in theo Maintenance Request ID
        /// </summary>
        [HttpGet("maintenance-request/{maintenanceRequestId}")]
        public async Task<IActionResult> GetVehicleCheckinsByMaintenanceRequestId(long maintenanceRequestId)
        {
            try
            {
                var result = await _vehicleCheckinService.GetVehicleCheckinsByMaintenanceRequestIdAsync(maintenanceRequestId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// X�a vehicle check-in
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Consulter")] // Chỉ Consulter được xóa check-in
        public async Task<IActionResult> DeleteVehicleCheckin(long id)
        {
            try
            {
                var result = await _vehicleCheckinService.DeleteVehicleCheckinAsync(id);
                if (result)
                {
                    return Ok(new { success = true, message = "Vehicle check-in deleted successfully" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Vehicle check-in not found" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Tìm kiếm xe theo biển số hoặc số khung
        /// </summary>
        [HttpGet("search-vehicle")]
        public async Task<IActionResult> SearchVehicle([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest(new { success = false, message = "Search term is required" });

                var result = await _vehicleCheckinService.SearchVehicleAsync(searchTerm);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Link vehicle check-in v?i maintenance request
        /// </summary>
        [HttpPut("{id}/link-maintenance")]
        public async Task<IActionResult> LinkMaintenanceRequest(long id, [FromBody] LinkMaintenanceRequestDto request)
        {
            try
            {
                var result = await _vehicleCheckinService.LinkMaintenanceRequestAsync(id, request.MaintenanceRequestId);
                return Ok(new { success = true, data = result, message = "Vehicle check-in linked to maintenance request successfully" });
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
        /// Cập nhật trạng thái VehicleCheckin (chỉ PENDING hoặc CONFIRMED)
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Consulter")] // Chỉ Consulter được cập nhật trạng thái
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusDto request)
        {
            try
            {
                // Validate status
                if (request.StatusCode != "PENDING" && request.StatusCode != "CONFIRMED")
                {
                    return BadRequest(new { success = false, message = "Trạng thái chỉ được phép là PENDING hoặc CONFIRMED" });
                }

                var result = await _vehicleCheckinService.UpdateStatusAsync(id, request.StatusCode);
                return Ok(new { success = true, data = result, message = "Cập nhật trạng thái thành công" });
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
    }

    /// <summary>
    /// DTO cho vi?c link maintenance request
    /// </summary>
    public class LinkMaintenanceRequestDto
    {
        [Required(ErrorMessage = "Maintenance Request ID is required")]
        public long MaintenanceRequestId { get; set; }
    }

    /// <summary>
    /// DTO cho việc cập nhật trạng thái
    /// </summary>
    public class UpdateStatusDto
    {
        [Required(ErrorMessage = "Status code is required")]
        public string StatusCode { get; set; } = string.Empty;
    }
}
