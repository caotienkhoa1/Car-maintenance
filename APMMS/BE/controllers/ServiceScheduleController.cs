using BE.DTOs.ServiceSchedule;

using BE.interfaces;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using System.Security.Claims;



namespace BE.controllers

{

    [ApiController]

    [Route("api/[controller]")]

    [Authorize]

    public class ServiceScheduleController : ControllerBase

    {

        private readonly IServiceScheduleService _serviceScheduleService;
        private readonly BE.repository.IRepository.IServiceScheduleRepository _scheduleRepository;
        private readonly BE.models.CarMaintenanceDbContext _context;



        public ServiceScheduleController(
            IServiceScheduleService serviceScheduleService,
            BE.repository.IRepository.IServiceScheduleRepository scheduleRepository,
            BE.models.CarMaintenanceDbContext context)
        {
            _serviceScheduleService = serviceScheduleService;
            _scheduleRepository = scheduleRepository;
            _context = context;
        }



        /// <summary>

        /// Đặt lịch công khai (khách hàng chưa có tài khoản)

        /// </summary>

        [HttpPost("public-booking")]

        [AllowAnonymous] // Public endpoint - không cần authentication

        public async Task<IActionResult> CreatePublicBooking([FromBody] PublicBookingDto request)

        {

            try

            {

                var result = await _serviceScheduleService.CreatePublicBookingAsync(request);

                return Ok(new { success = true, data = result, message = "Đặt lịch thành công! Chúng tôi sẽ liên hệ với bạn sớm nhất." });

            }

            catch (ArgumentException ex)

            {

                return BadRequest(new { success = false, message = ex.Message });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = "Lỗi hệ thống, vui lòng thử lại sau", error = ex.Message });

            }

        }



        /// <summary>

        /// Tạo lịch hẹn mới (khách hàng đặt lịch)

        /// </summary>

        [HttpPost]

        public async Task<IActionResult> CreateSchedule([FromBody] RequestDto request)

        {

            try

            {

                // Get current user ID from JWT token for audit trail

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                long? currentUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

                var result = await _serviceScheduleService.CreateScheduleAsync(request, currentUserId);

                return Ok(new { success = true, data = result, message = "Schedule created successfully" });

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

        /// Lấy danh sách ghi chú của lịch hẹn

        /// </summary>

        [HttpGet("{id}/notes")]

        [Authorize(Roles = "Admin,Branch Manager,Consulter")]

        public async Task<IActionResult> GetNotes(long id)

        {

            try

            {

                var result = await _serviceScheduleService.GetNotesAsync(id);

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

        /// Thêm ghi chú vào lịch hẹn

        /// </summary>

        [HttpPost("{id}/notes")]

        [Authorize(Roles = "Admin,Branch Manager,Consulter")]

        public async Task<IActionResult> AddNote(long id, [FromBody] AddNoteDto request)

        {

            if (request == null)

            {

                return BadRequest(new { success = false, message = "Request body is required" });

            }



            try

            {

                // Get current user ID from JWT token for audit trail

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                long? currentUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

                var result = await _serviceScheduleService.AddNoteAsync(id, request, currentUserId);

                return Ok(new { success = true, data = result, message = "Note added successfully" });

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

        /// Lấy lịch hẹn theo ID

        /// </summary>

        [HttpGet("{id}")]

        public async Task<IActionResult> GetScheduleById(long id)

        {

            try

            {

                var result = await _serviceScheduleService.GetScheduleByIdAsync(id);

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

        /// Lấy danh sách tất cả lịch hẹn (có phân trang)

        /// Nếu user có branchId, chỉ trả về lịch hẹn của chi nhánh đó

        /// </summary>

        [HttpGet]

        public async Task<IActionResult> GetAllSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] long? branchId = null)

        {

            try

            {

                // Nếu không có branchId trong query, thử lấy từ JWT token

                if (!branchId.HasValue && User.Identity?.IsAuthenticated == true)

                {

                    var branchIdClaim = User.FindFirst("BranchId")?.Value;

                    if (long.TryParse(branchIdClaim, out var userBranchId))

                    {

                        branchId = userBranchId;

                    }

                }



                // Nếu có branchId, filter theo branchId

                if (branchId.HasValue)

                {

                    var result = await _serviceScheduleService.GetSchedulesByBranchIdAsync(branchId.Value);

                    return Ok(new { success = true, data = result });

                }



                var allResult = await _serviceScheduleService.GetAllSchedulesAsync(page, pageSize);

                return Ok(new { success = true, data = allResult });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });

            }

        }



        /// <summary>

        /// Lấy danh sách lịch hẹn theo User ID (lịch hẹn của khách hàng)

        /// </summary>

        [HttpGet("by-user/{userId}")]

        public async Task<IActionResult> GetSchedulesByUserId(long userId)

        {

            try

            {

                var result = await _serviceScheduleService.GetSchedulesByUserIdAsync(userId);

                return Ok(new { success = true, data = result });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });

            }

        }



        /// <summary>

        /// Lấy danh sách lịch hẹn theo Branch ID

        /// </summary>

        [HttpGet("by-branch/{branchId}")]

        public async Task<IActionResult> GetSchedulesByBranchId(long branchId)

        {

            try

            {

                var result = await _serviceScheduleService.GetSchedulesByBranchIdAsync(branchId);

                return Ok(new { success = true, data = result });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });

            }

        }



        /// <summary>

        /// Lấy danh sách lịch hẹn theo Status

        /// Nếu user có branchId, chỉ trả về lịch hẹn của chi nhánh đó

        /// </summary>

        [HttpGet("by-status/{statusCode}")]

        public async Task<IActionResult> GetSchedulesByStatus(string statusCode, [FromQuery] long? branchId = null)

        {

            try

            {

                // Nếu không có branchId trong query, thử lấy từ JWT token

                if (!branchId.HasValue && User.Identity?.IsAuthenticated == true)

                {

                    var branchIdClaim = User.FindFirst("BranchId")?.Value;

                    if (long.TryParse(branchIdClaim, out var userBranchId))

                    {

                        branchId = userBranchId;

                    }

                }



                var result = await _serviceScheduleService.GetSchedulesByStatusAsync(statusCode, branchId);

                return Ok(new { success = true, data = result });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });

            }

        }

        /// <summary>
        /// Lấy danh sách lịch hẹn hôm nay (dùng cho thông báo/dashboard)
        /// Nếu user có branchId, chỉ trả về lịch hẹn của chi nhánh đó
        /// </summary>
        [HttpGet("today")]
        public async Task<IActionResult> GetTodaySchedules([FromQuery] long? branchId = null)
        {
            try
            {
                // Nếu không có branchId trong query, thử lấy từ JWT token
                if (!branchId.HasValue && User.Identity?.IsAuthenticated == true)
                {
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (long.TryParse(branchIdClaim, out var userBranchId))
                    {
                        branchId = userBranchId;
                    }
                }

                var schedules = await _serviceScheduleService.GetTodaySchedulesAsync(branchId);
                var count = await _serviceScheduleService.GetTodaySchedulesCountAsync(branchId);

                return Ok(new { 
                    success = true, 
                    data = schedules,
                    count = count,
                    message = $"Hôm nay có {count} lịch hẹn"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Đếm số lượng lịch hẹn hôm nay (dùng cho badge notification)
        /// </summary>
        [HttpGet("today/count")]
        public async Task<IActionResult> GetTodaySchedulesCount([FromQuery] long? branchId = null)
        {
            try
            {
                // Nếu không có branchId trong query, thử lấy từ JWT token
                if (!branchId.HasValue && User.Identity?.IsAuthenticated == true)
                {
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (long.TryParse(branchIdClaim, out var userBranchId))
                    {
                        branchId = userBranchId;
                    }
                }

                var count = await _serviceScheduleService.GetTodaySchedulesCountAsync(branchId);
                return Ok(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>

        /// Lấy danh sách lịch hẹn theo khoảng thời gian

        /// Nếu user có branchId, chỉ trả về lịch hẹn của chi nhánh đó

        /// </summary>

        [HttpGet("by-date-range")]

        public async Task<IActionResult> GetSchedulesByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] long? branchId = null)

        {

            try

            {

                // Nếu không có branchId trong query, thử lấy từ JWT token

                if (!branchId.HasValue && User.Identity?.IsAuthenticated == true)

                {

                    var branchIdClaim = User.FindFirst("BranchId")?.Value;

                    if (long.TryParse(branchIdClaim, out var userBranchId))

                    {

                        branchId = userBranchId;

                    }

                }



                var result = await _serviceScheduleService.GetSchedulesByDateRangeAsync(startDate, endDate, branchId);

                return Ok(new { success = true, data = result });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });

            }

        }



        /// <summary>

        /// Hủy lịch hẹn

        /// </summary>

        [HttpPut("{id}/cancel")]

        [Authorize(Roles = "Admin,Branch Manager,Consulter")]

        public async Task<IActionResult> CancelSchedule(long id, [FromBody] CancelScheduleDto? request = null)

        {

            try

            {

                // Get current user ID from JWT token for audit trail

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                long? currentUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

                var result = await _serviceScheduleService.CancelScheduleAsync(id, request, currentUserId);

                return Ok(new { success = true, data = result, message = "Schedule cancelled successfully" });

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

        /// AutoOwner hủy lịch hẹn của chính họ

        /// </summary>

        [HttpPut("{id}/cancel-by-owner")]

        [Authorize(Roles = "Auto Owner,AutoOwner")]

        public async Task<IActionResult> CancelScheduleByOwner(long id, [FromBody] CancelScheduleDto? request = null)

        {

            try

            {

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var currentUserId))

                {

                    return Unauthorized(new { success = false, message = "Invalid user token" });

                }



                var schedule = await _scheduleRepository.GetByIdAsync(id);

                if (schedule == null)

                {

                    return NotFound(new { success = false, message = "Schedule not found" });

                }



                var isOwner = schedule.UserId == currentUserId;

                if (!isOwner && schedule.GuestId.HasValue)

                {

                    await _context.Entry(schedule).Reference(s => s.Guest).LoadAsync();

                    if (schedule.Guest?.LinkedUserId == currentUserId)

                    {

                        isOwner = true;

                    }

                }



                if (!isOwner)

                {

                    return Forbid("You can only cancel your own schedules");

                }



                var result = await _serviceScheduleService.CancelScheduleAsync(id, request, currentUserId);

                return Ok(new { success = true, data = result, message = "Schedule cancelled successfully" });

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

        /// Hoàn thành lịch hẹn

        /// </summary>

        [HttpPut("{id}/complete")]

        [Authorize(Roles = "Admin,Branch Manager,Consulter")]

        public async Task<IActionResult> CompleteSchedule(long id)

        {

            try

            {

                // Get current user ID from JWT token for audit trail

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                long? currentUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

                var result = await _serviceScheduleService.CompleteScheduleAsync(id, currentUserId);

                return Ok(new { success = true, data = result, message = "Schedule completed successfully" });

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

        /// Cập nhật lịch hẹn

        /// </summary>

        [HttpPut("{id}")]

        [Authorize(Roles = "Admin,Branch Manager,Consulter")]

        public async Task<IActionResult> UpdateSchedule(long id, [FromBody] UpdateScheduleDto request)

        {

            try

            {

                // Get current user ID from JWT token for audit trail

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                long? currentUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

                var result = await _serviceScheduleService.UpdateScheduleAsync(id, request, currentUserId);

                return Ok(new { success = true, data = result, message = "Schedule updated successfully" });

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

        /// Consultant accepts a schedule

        /// </summary>

        [HttpPut("{id}/accept")]

        [Authorize(Roles = "Admin,Branch Manager,Consulter")]

        public async Task<IActionResult> AcceptSchedule(long id, [FromBody] AcceptScheduleDto request)

        {

            if (request == null)

            {

                return BadRequest(new { success = false, message = "Request body is required" });

            }



            try

            {

                // Get current user ID from JWT token for audit trail

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                long? currentUserId = long.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

                var result = await _serviceScheduleService.AcceptScheduleAsync(id, request, currentUserId);

                return Ok(new { success = true, data = result, message = "Schedule accepted successfully" });

            }

            catch (ArgumentException ex)

            {

                return BadRequest(new { success = false, message = ex.Message });

            }

            catch (InvalidOperationException ex)

            {

                return Conflict(new { success = false, message = ex.Message });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });

            }

        }



        /// <summary>

        /// Xóa lịch hẹn

        /// </summary>

        [HttpDelete("{id}")]

        [Authorize(Roles = "Admin,Branch Manager")]

        public async Task<IActionResult> DeleteSchedule(long id)

        {

            try

            {

                var result = await _serviceScheduleService.DeleteScheduleAsync(id);

                if (result)

                {

                    return Ok(new { success = true, message = "Schedule deleted successfully" });

                }

                else

                {

                    return NotFound(new { success = false, message = "Schedule not found" });

                }

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });

            }

        }

    }

}


