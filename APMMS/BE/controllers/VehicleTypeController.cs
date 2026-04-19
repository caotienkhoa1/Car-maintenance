using BE.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleTypeController : ControllerBase
    {
        private readonly CarMaintenanceDbContext _context;

        public VehicleTypeController(CarMaintenanceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var vehicleTypes = await _context.VehicleTypes
                    .OrderBy(vt => vt.Name)
                    .Select(vt => new
                    {
                        id = vt.Id,
                        name = vt.Name,
                        description = vt.Description
                    })
                    .ToListAsync();

                return Ok(vehicleTypes);
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
                var vehicleType = await _context.VehicleTypes
                    .Where(vt => vt.Id == id)
                    .Select(vt => new
                    {
                        id = vt.Id,
                        name = vt.Name,
                        description = vt.Description
                    })
                    .FirstOrDefaultAsync();

                if (vehicleType == null)
                    return NotFound(new { success = false, message = "Vehicle type not found" });

                return Ok(vehicleType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }
}

