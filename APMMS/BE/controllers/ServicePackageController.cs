using BE.DTOs.ServicePackage;
using BE.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicePackageController : ControllerBase
    {
        private readonly IServicePackageService _service;
        public ServicePackageController(IServicePackageService service)
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
                // Lấy userId từ JWT token
                long? userId = null;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                // Nếu có BranchId trong JWT claim, ưu tiên dùng nó
                if (!branchId.HasValue)
                {
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (long.TryParse(branchIdClaim, out var claimBranchId))
                    {
                        branchId = claimBranchId;
                    }
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
                if (item == null) return NotFound(new { success = false, message = "ServicePackage not found" });
                return Ok(new { success = true, data = item });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] RequestDto dto)
        {
            try
            {
                // Lấy BranchId từ JWT để đảm bảo gói dịch vụ thuộc chi nhánh của user hiện tại
                long? branchId = null;
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                if (!string.IsNullOrEmpty(branchIdClaim) && long.TryParse(branchIdClaim, out var claimBranchId))
                {
                    branchId = claimBranchId;
                }

                if (!branchId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Không thể xác định chi nhánh của người dùng khi tạo gói dịch vụ." });
                }

                dto.BranchId = branchId;

                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, new { success = true, data = created, message = "ServicePackage created successfully" });
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

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(long id, [FromBody] RequestDto dto)
        {
            try
            {
                dto.Id = id;

                // Lấy BranchId từ JWT để đảm bảo chỉ được cập nhật gói trong chi nhánh của mình
                long? branchId = null;
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                if (!string.IsNullOrEmpty(branchIdClaim) && long.TryParse(branchIdClaim, out var claimBranchId))
                {
                    branchId = claimBranchId;
                }

                if (!branchId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Không thể xác định chi nhánh của người dùng khi cập nhật gói dịch vụ." });
                }

                dto.BranchId = branchId;

                var updated = await _service.UpdateAsync(dto);
                if (updated == null) return NotFound(new { success = false, message = "ServicePackage not found" });
                return Ok(new { success = true, data = updated, message = "ServicePackage updated successfully" });
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
        public async Task<IActionResult> SetStatus(long id, [FromQuery] string statusCode)
        {
            try
            {
                await _service.DisableEnableAsync(id, statusCode);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }
}
