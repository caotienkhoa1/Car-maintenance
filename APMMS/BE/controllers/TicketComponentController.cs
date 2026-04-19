using BE.DTOs.TicketComponent;
using BE.interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketComponentController : ControllerBase
    {
        private readonly ITicketComponentService _service;

        public TicketComponentController(ITicketComponentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách phụ tùng theo Maintenance Ticket ID
        /// </summary>
        [HttpGet("by-maintenance-ticket/{maintenanceTicketId}")]
        public async Task<IActionResult> GetByMaintenanceTicketId(long maintenanceTicketId)
        {
            try
            {
                var result = await _service.GetByMaintenanceTicketIdAsync(maintenanceTicketId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết phụ tùng theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                    return NotFound(new { success = false, message = "Ticket component not found" });
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Thêm phụ tùng vào Maintenance Ticket
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestDto dto)
        {
            try
            {
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                long? userId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;
                
                var result = await _service.CreateAsync(dto, userId);
                return Ok(new { success = true, data = result, message = "Component added successfully" });
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
        /// Cập nhật phụ tùng
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] RequestDto dto)
        {
            try
            {
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                long? userId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;
                
                var result = await _service.UpdateAsync(id, dto, userId);
                if (result == null)
                    return NotFound(new { success = false, message = "Ticket component not found" });
                return Ok(new { success = true, data = result, message = "Component updated successfully" });
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
        /// Xóa phụ tùng khỏi Maintenance Ticket
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound(new { success = false, message = "Ticket component not found" });
                return Ok(new { success = true, message = "Component removed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Tính tổng chi phí phụ tùng của Maintenance Ticket
        /// </summary>
        [HttpGet("by-maintenance-ticket/{maintenanceTicketId}/total-cost")]
        public async Task<IActionResult> GetTotalCost(long maintenanceTicketId)
        {
            try
            {
                var totalCost = await _service.CalculateTotalCostAsync(maintenanceTicketId);
                return Ok(new { success = true, data = new { totalCost } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }
}

