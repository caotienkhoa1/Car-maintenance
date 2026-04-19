using Microsoft.AspNetCore.Mvc;
using BE.DTOs.Branch;
using BE.interfaces;
using System.Linq;
using System.Security.Claims;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var branches = await _branchService.GetAllAsync();
                return Ok(new { success = true, data = branches });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var branch = await _branchService.GetByIdAsync(id);
                if (branch == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy chi nhánh" });
                }
                return Ok(new { success = true, data = branch });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BranchRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errorsByField = ModelState
                        .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => 
                                string.IsNullOrEmpty(e.ErrorMessage) 
                                    ? e.Exception?.Message ?? "Invalid value" 
                                    : e.ErrorMessage).ToArray()
                        );
                    
                    var errorsList = errorsByField
                        .SelectMany(kvp => kvp.Value.Select(e => e))
                        .ToList();
                    
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = errorsByField, errorsList = errorsList });
                }

                var branch = await _branchService.CreateAsync(dto);
                return Ok(new { success = true, data = branch, message = "Tạo chi nhánh thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] BranchRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errorsByField = ModelState
                        .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => 
                                string.IsNullOrEmpty(e.ErrorMessage) 
                                    ? e.Exception?.Message ?? "Invalid value" 
                                    : e.ErrorMessage).ToArray()
                        );
                    
                    var errorsList = errorsByField
                        .SelectMany(kvp => kvp.Value.Select(e => e))
                        .ToList();
                    
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = errorsByField, errorsList = errorsList });
                }

                var branch = await _branchService.UpdateAsync(id, dto);
                if (branch == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy chi nhánh" });
                }

                return Ok(new { success = true, data = branch, message = "Cập nhật chi nhánh thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _branchService.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy chi nhánh" });
                }

                return Ok(new { success = true, message = "Xóa chi nhánh thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}/Director")]
        public async Task<IActionResult> GetDirector(long id)
        {
            try
            {
                var director = await _branchService.GetDirectorAsync(id);
                if (director == null)
                {
                    return Ok(new { success = true, data = (object?)null, message = "Chi nhánh chưa có giám đốc" });
                }
                return Ok(new { success = true, data = director });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// ✅ Tạo giám đốc chi nhánh mới (độc lập với Employee)
        /// </summary>
        [HttpPost("{id}/Director")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateDirector(long id, [FromBody] CreateDirectorDto dto)
        {
            try
            {
                // Lấy userId từ JWT
                long? createdByUserId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    createdByUserId = parsedUserId;
                }

                var result = await _branchService.CreateDirectorAsync(id, dto, createdByUserId);
                return Ok(new { success = true, message = "Tạo giám đốc thành công", data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
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
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}/Director/{directorId}")]
        public async Task<IActionResult> ChangeDirector(long id, long directorId)
        {
            try
            {
                var result = await _branchService.ChangeDirectorAsync(id, directorId);
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Không thể thay đổi giám đốc" });
                }

                return Ok(new { success = true, message = "Thay đổi giám đốc thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
