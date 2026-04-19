using Microsoft.AspNetCore.Mvc;
using BE.interfaces;
using Microsoft.AspNetCore.Authorization;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HomeController : ControllerBase
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] long? branchId = null)
        {
            try
            {
                var stats = await _homeService.GetDashboardStatsAsync(branchId);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }
}
