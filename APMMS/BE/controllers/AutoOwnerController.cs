using BE.DTOs.AutoOwner;
using BE.services;
using BE.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.interfaces;
using System.IO;

namespace BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu đăng nhập cho tất cả endpoint quản lý khách hàng
    public class AutoOwnerController : ControllerBase
    {
        private readonly IAutoOwnerService _service;
        private readonly CloudinaryService _cloudinaryService;
        private readonly CarMaintenanceDbContext _dbContext;

        public AutoOwnerController(IAutoOwnerService service, CloudinaryService cloudinaryService, CarMaintenanceDbContext dbContext)
        {
            _service = service;
            _cloudinaryService = cloudinaryService;
            _dbContext = dbContext;
        }

        [HttpGet]
        [Authorize(Roles = "Branch Manager,Admin,Consulter")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] long? role = null)
        {
            try
            {
                // Normalize empty strings to null
                if (string.IsNullOrWhiteSpace(search)) search = null;
                if (string.IsNullOrWhiteSpace(status)) status = null;
                
                // Nếu có search, status hoặc role thì dùng filter
                if (search != null || status != null || role.HasValue)
                {
                    var result = await _service.GetWithFiltersAsync(page, pageSize, search, status, role);
                    return Ok(result);
                }
                
                // Nếu không có filter thì dùng method cũ nhưng vẫn trả về format đầy đủ
                var users = await _service.GetAllAsync(page, pageSize);
                var totalCount = await _service.GetTotalCountAsync(null, null, null);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                return Ok(new { 
                    success = true, 
                    data = users, 
                    page = page, 
                    pageSize = pageSize,
                    totalPages = totalPages,
                    currentPage = page,
                    totalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tải dữ liệu: " + ex.Message });
            }
        }

        [HttpGet("{id:long}")]
        [Authorize(Roles = "Branch Manager,Admin,Consulter")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound("Auto Owner not found.");
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Consulter")]
        public async Task<IActionResult> Create([FromBody] RequestDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "Consulter")]
        public async Task<IActionResult> Update(long id, [FromBody] RequestDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// ✅ Cập nhật Status của AutoOwner (ACTIVE hoặc INACTIVE)
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Consulter")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var result = await _service.UpdateStatusAsync(id, request.StatusCode);
                return Ok(new { success = true, data = result, message = "Status updated successfully" });
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
        /// Upload avatar image to Cloudinary for AutoOwner (cho phép Admin/Branch Manager upload ảnh cho customer theo ID)
        /// </summary>
        [HttpPost("{id}/upload-avatar")]
        [Authorize(Roles = "Consulter")]
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

                // Kiểm tra user có tồn tại không
                var user = await _dbContext.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "AutoOwner not found" });
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
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }

    public class UpdateStatusRequest
    {
        public string StatusCode { get; set; } = null!;
    }
}
