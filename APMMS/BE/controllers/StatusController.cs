using Microsoft.AspNetCore.Mvc;
using BE.models;
using Microsoft.EntityFrameworkCore;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly CarMaintenanceDbContext _context;

        public StatusController(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var statuses = await _context.StatusLookups.ToListAsync();
                return Ok(new { success = true, data = statuses });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
