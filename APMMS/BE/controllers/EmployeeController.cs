using BE.DTOs.Employee;
using BE.interfaces;
using BE.services;
using BE.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;

namespace BE.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly CloudinaryService _cloudinaryService;
        private readonly CarMaintenanceDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;

        public EmployeeController(IEmployeeService employeeService, CloudinaryService cloudinaryService, CarMaintenanceDbContext dbContext, IWebHostEnvironment environment)
        {
            _employeeService = employeeService;
            _cloudinaryService = cloudinaryService;
            _dbContext = dbContext;
            _environment = environment;
        }

        [HttpGet]
        [Authorize(Roles = "Branch Manager,Admin")] // Admin xem tất cả, BM xem chi nhánh của mình
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] long? role = null,
            [FromQuery] long? branchId = null)
        {
            // Nếu là Branch Manager thì ép branchId về chi nhánh của chính họ
            var roleIdClaim = User.FindFirst("RoleId")?.Value;
            var roleParseSuccess = long.TryParse(roleIdClaim, out var roleIdVal);
            var isBranchManager = roleParseSuccess && roleIdVal == 2;
            if (isBranchManager)
            {
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                if (!long.TryParse(branchIdClaim, out var managerBranchId))
                {
                    return BadRequest(new { success = false, message = "Không xác định được chi nhánh của người dùng hiện tại." });
                }
                branchId = managerBranchId; // ép filter theo chi nhánh của BM
            }

            // Luôn dùng filter method để đảm bảo format response đồng nhất
            var result = await _employeeService.GetWithFiltersAsync(page, pageSize, search, status, role, branchId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Branch Manager,Admin")] // Admin xem tất cả, BM xem chi nhánh của mình
        public async Task<IActionResult> GetById(long id)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null) return NotFound(new { success = false, message = "Employee not found" });

            // Nếu là Branch Manager thì chỉ xem được nhân viên trong chi nhánh của mình
            var roleIdClaim = User.FindFirst("RoleId")?.Value;
            var roleId = long.TryParse(roleIdClaim, out var parsedRoleId) ? parsedRoleId : 0;
            if (roleId == 2) // Branch Manager
                {
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (!long.TryParse(branchIdClaim, out var branchId) || employee.BranchId != branchId)
                    {
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền xem thông tin nhân viên thuộc chi nhánh khác." });
                    }
                }

            return Ok(employee);
        }

        [HttpPost]
        [Authorize(Roles = "Branch Manager,Admin")] // ✅ Admin và Giám đốc chi nhánh đều được tạo nhân viên
        public async Task<IActionResult> Create([FromBody] EmployeeRequestDto dto)
        {
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
                }

                // ✅ Kiểm tra quyền: Admin có thể tạo nhân viên cho bất kỳ chi nhánh nào, Branch Manager chỉ tạo cho chi nhánh của mình
                var isAdmin = User.IsInRole("Admin");
                long? branchId = null;

                if (isAdmin)
                {
                    // ✅ Admin: Sử dụng branchId từ request body (nếu có), nếu không có thì lấy từ JWT
                    branchId = dto.BranchId;
                    if (!branchId.HasValue)
                    {
                        var branchIdClaim = User.FindFirst("BranchId")?.Value;
                        if (long.TryParse(branchIdClaim, out var claimBranchId))
                        {
                            branchId = claimBranchId;
                        }
                    }
                    
                    // ✅ Nếu Admin không có branchId trong request và không có trong JWT, báo lỗi
                    if (!branchId.HasValue)
                    {
                        return BadRequest(new { success = false, message = "Vui lòng chỉ định chi nhánh cho nhân viên mới." });
                    }
                }
                else
                {
                    // ✅ Branch Manager: Chỉ cho phép tạo nhân viên trong chi nhánh của mình
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (!long.TryParse(branchIdClaim, out var managerBranchId))
                {
                    return BadRequest(new { success = false, message = "Không xác định được chi nhánh của người dùng hiện tại." });
                }
                    branchId = managerBranchId; // ✅ Ép branchId về chi nhánh của Branch Manager
                }
                
                dto.BranchId = branchId;
                
                // Get current user ID from JWT claims for audit log
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                long? createdByUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

                var result = await _employeeService.CreateAsync(dto, createdByUserId);
                return Ok(new { success = true, data = result, message = "Employee created successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                var isDevelopment = _environment.IsDevelopment();
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Internal server error", 
                    error = ex.Message,
                    stackTrace = isDevelopment ? ex.StackTrace : null,
                    innerException = isDevelopment ? ex.InnerException?.Message : null
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Branch Manager")] // Chỉ Giám đốc chi nhánh mới được sửa nhân viên
        public async Task<IActionResult> Update(long id, [FromBody] EmployeeRequestDto dto)
        {
            try
            {
                // Validate model (chỉ validate các field được gửi lên)
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
                }

                // Lấy BranchId của Giám đốc từ JWT, chỉ cho phép sửa nhân viên trong chi nhánh của mình
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                if (!long.TryParse(branchIdClaim, out var managerBranchId))
                {
                    return BadRequest(new { success = false, message = "Không xác định được chi nhánh của người dùng hiện tại." });
                }

                // Kiểm tra nhân viên thuộc chi nhánh nào
                var existing = await _employeeService.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { success = false, message = "Employee not found" });
                }
                if (existing.BranchId != managerBranchId)
                {
                    return StatusCode(403, new { success = false, message = "Bạn chỉ được phép chỉnh sửa nhân viên thuộc chi nhánh của mình." });
                }

                // Cố định BranchId về chi nhánh của Giám đốc
                dto.BranchId = managerBranchId;

                // Get current user ID from JWT claims for audit log
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                long? modifiedByUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

                var result = await _employeeService.UpdateAsync(id, dto, modifiedByUserId);
                if (result == null) return NotFound(new { success = false, message = "Employee not found" });
                return Ok(new { success = true, data = result, message = "Employee updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                var isDevelopment = _environment.IsDevelopment();
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Internal server error", 
                    error = ex.Message,
                    stackTrace = isDevelopment ? ex.StackTrace : null
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Branch Manager")] // Chỉ Giám đốc chi nhánh mới được soft-delete
        public async Task<IActionResult> Delete(long id)
        {
            // Lấy BranchId của Giám đốc
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            if (!long.TryParse(branchIdClaim, out var managerBranchId))
            {
                return BadRequest(new { success = false, message = "Không xác định được chi nhánh của người dùng hiện tại." });
            }

            // Kiểm tra nhân viên thuộc chi nhánh nào
            var existing = await _employeeService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { success = false, message = "Employee not found" });
            }
            if (existing.BranchId != managerBranchId)
            {
                return StatusCode(403, new { success = false, message = "Bạn chỉ được phép xoá nhân viên thuộc chi nhánh của mình." });
            }

            var success = await _employeeService.DeleteAsync(id);
            if (!success) return NotFound();
            return Ok(new { message = "Employee deleted successfully (soft delete)." });
        }
        
        [HttpGet("filter")]
        [Authorize] // Cho phép tất cả user đã đăng nhập (bao gồm Consulter) truy cập để lấy danh sách technician
        public async Task<IActionResult> FilterEmployees([FromQuery] bool? isDelete, [FromQuery] long? roleId, [FromQuery] long? branchId = null)
        {
            var employees = await _employeeService.FilterAsync(isDelete, roleId, branchId);
            return Ok(employees);
        }

        /// <summary>
        /// Trả về thông tin cơ bản của employee hiện tại (dùng để lấy Branch cho UI). Accessible cho mọi user đã đăng nhập.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            // Dùng service để lấy thông tin employee theo id, hoặc truy vấn tối thiểu từ DbContext
            var emp = await _employeeService.GetByIdAsync(userId);
            if (emp == null)
            {
                return NotFound(new { success = false, message = "Employee not found" });
            }

            return Ok(new { success = true, data = new { id = userId, branchId = emp.BranchId, branchName = emp.BranchName } });
        }
        
        /// <summary>
        /// ✅ Cập nhật Status của Employee (ACTIVE hoặc INACTIVE)
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Branch Manager")] // Chỉ Giám đốc chi nhánh được đổi trạng thái nhân viên
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] EmployeeUpdateStatusDto request)
        {
            try
            {
                // Lấy BranchId của Giám đốc
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                if (!long.TryParse(branchIdClaim, out var managerBranchId))
                {
                    return BadRequest(new { success = false, message = "Không xác định được chi nhánh của người dùng hiện tại." });
                }

                // Kiểm tra nhân viên thuộc chi nhánh nào
                var existing = await _employeeService.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { success = false, message = "Employee not found" });
                }
                if (existing.BranchId != managerBranchId)
                {
                    return Forbid("Bạn chỉ được phép cập nhật trạng thái nhân viên thuộc chi nhánh của mình.");
                }

                var result = await _employeeService.UpdateStatusAsync(id, request.StatusCode);
                return Ok(new { success = true, data = result, message = "Status updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                var isDevelopment = _environment.IsDevelopment();
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Internal server error", 
                    error = ex.Message,
                    stackTrace = isDevelopment ? ex.StackTrace : null
                });
            }
        }

        /// <summary>
        /// Upload avatar image to Cloudinary for employee (cho phép Admin/Branch Manager upload ảnh cho employee theo ID)
        /// </summary>
        [HttpPost("{id}/upload-avatar")]
        [Authorize(Roles = "Branch Manager")] // Chỉ Giám đốc chi nhánh được cập nhật avatar cho nhân viên
        public async Task<IActionResult> UploadAvatar(long id, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Không có file được chọn" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp)" });
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "Kích thước file không được vượt quá 5MB" });
                }

                // Lấy BranchId của Giám đốc
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                if (!long.TryParse(branchIdClaim, out var managerBranchId))
                {
                    return BadRequest(new { success = false, message = "Không xác định được chi nhánh của người dùng hiện tại." });
                }

                // Kiểm tra employee có tồn tại và thuộc chi nhánh của Giám đốc không
                var user = await _dbContext.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Employee not found" });
                }
                if (user.BranchId != managerBranchId)
                {
                    return Forbid("Bạn chỉ được phép cập nhật ảnh cho nhân viên thuộc chi nhánh của mình.");
                }

                // Upload to Cloudinary (isAvatar = true để crop thành hình vuông)
                var imageUrl = await _cloudinaryService.UploadImageAsync(file, "user-avatars", isAvatar: true);

                // Cập nhật image URL vào database
                user.Image = imageUrl;
                user.LastModifiedDate = DateTime.Now;
                
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, data = new { imageUrl = imageUrl }, message = "Upload avatar thành công" });
            }
            catch (Exception ex)
            {
                var isDevelopment = _environment.IsDevelopment();
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Internal server error", 
                    error = ex.Message,
                    stackTrace = isDevelopment ? ex.StackTrace : null
                });
            }
        }

    }
}
