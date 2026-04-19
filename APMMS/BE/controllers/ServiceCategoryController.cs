using Microsoft.AspNetCore.Mvc;
using BE.models;
using Microsoft.EntityFrameworkCore;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceCategoryController : ControllerBase
    {
        private readonly CarMaintenanceDbContext _context;

        public ServiceCategoryController(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? statusCode = null)
        {
            try
            {
                var query = _context.ServiceCategories.AsQueryable();
                
                // Lọc theo statusCode nếu có
                if (!string.IsNullOrEmpty(statusCode))
                {
                    query = query.Where(s => s.StatusCode == statusCode);
                }
                else
                {
                    // Mặc định chỉ lấy các dịch vụ đang hoạt động (statusCode != "DISABLED" hoặc null)
                    query = query.Where(s => s.StatusCode == null || s.StatusCode != "DISABLED");
                }

                var categories = await query
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        description = s.Description,
                        statusCode = s.StatusCode
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = categories });
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
                var category = await _context.ServiceCategories
                    .Where(s => s.Id == id)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        description = s.Description,
                        statusCode = s.StatusCode
                    })
                    .FirstOrDefaultAsync();

                if (category == null)
                    return NotFound(new { success = false, message = "Service category not found" });

                return Ok(new { success = true, data = category });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }
}

