using BE.DTOs.TypeComponent;
using BE.interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;

namespace BE.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TypeComponentController : ControllerBase
    {
        private readonly ITypeComponentService _service;
        private readonly BE.repository.IRepository.IUserRepository _userRepository;

        public TypeComponentController(ITypeComponentService service, BE.repository.IRepository.IUserRepository userRepository)
        {
            _service = service;
            _userRepository = userRepository;
        }

        [HttpGet]
        [Route("")] // ✅ Route cụ thể để tránh ambiguous match
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? statusCode = null,
            [FromQuery] long? branchId = null)
        {
            try
            {
                // Lấy userId từ JWT token
                long? userId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                // ✅ Kiểm tra quyền: Admin có thể xem tất cả, Branch Manager chỉ xem chi nhánh của mình
                var isAdmin = User.IsInRole("Admin");
                var isBranchManager = User.IsInRole("Branch Manager");
                
                // ✅ Lưu branchId từ query parameter trước khi xử lý
                var originalBranchIdFromQuery = branchId;
                
                // ✅ Branch Manager: Chỉ xem được chi nhánh của mình, ép branchId về chi nhánh của chính họ
                if (isBranchManager)
                {
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (!long.TryParse(branchIdClaim, out var managerBranchId))
                    {
                        return BadRequest(new { success = false, message = "Không xác định được chi nhánh của người dùng hiện tại." });
                    }
                    branchId = managerBranchId; // ✅ Ép filter theo chi nhánh của Branch Manager, bỏ qua query parameter
                }
                // ✅ Admin: Nếu có branchId trong query parameter, dùng nó để filter; nếu không có, xem tất cả (branchId = null)
                else if (!isAdmin)
                {
                    // User thường: LUÔN lấy BranchId từ JWT claim để đảm bảo chỉ hiển thị loại linh kiện của branch đó
                    // Bỏ qua branchId từ query parameter để tránh user có thể xem loại linh kiện của branch khác
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (long.TryParse(branchIdClaim, out var claimBranchId))
                    {
                        branchId = claimBranchId;
                    }
                    // Nếu không có branchId trong JWT, không trả về dữ liệu (hoặc có thể throw exception tùy yêu cầu)
                    // Để đảm bảo an toàn, nếu không có branchId thì set branchId = -1 để không trả về gì
                    if (!branchId.HasValue)
                    {
                        branchId = -1; // Sẽ không có loại linh kiện nào có branchId = -1
                    }
                }
                // ✅ Admin: branchId từ query parameter đã được giữ nguyên ở trên (có thể null để xem tất cả)

                System.Diagnostics.Debug.WriteLine($"[TypeComponentController.GetAll] isAdmin: {isAdmin}, isBranchManager: {isBranchManager}, originalBranchIdFromQuery: {originalBranchIdFromQuery}, finalBranchId: {branchId}");

                var result = await _service.GetAllAsync(page, pageSize, branchId, statusCode, search);
                var totalCount = await _service.GetTotalCountAsync(branchId, statusCode, search);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                System.Diagnostics.Debug.WriteLine($"[TypeComponentController.GetAll] Returned {result.Count} items, totalCount: {totalCount}");

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

        [HttpGet("{id:long}")] // ✅ Constraint để chỉ match với số, tránh ambiguous match
        public async Task<IActionResult> Get(long id)
        {
            try
            {
                var item = await _service.GetByIdAsync(id);
                if (item == null) return NotFound(new { success = false, message = "TypeComponent not found" });
                return Ok(new { success = true, data = item });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestDto dto)
        {
            try
            {
                // LUÔN lấy BranchId từ JWT claim của user đang đăng nhập
                // Bỏ qua branchId từ request body để đảm bảo user chỉ có thể tạo loại linh kiện cho branch của mình
                long? branchId = null;
                
                // Thử lấy từ JWT claim trước
                var branchIdClaim = User.FindFirst("BranchId")?.Value;
                Console.WriteLine($"[TypeComponentController.Create] BranchId from JWT claim: {branchIdClaim}");
                
                if (!string.IsNullOrEmpty(branchIdClaim) && long.TryParse(branchIdClaim, out var claimBranchId))
                {
                    branchId = claimBranchId;
                    Console.WriteLine($"[TypeComponentController.Create] Using branchId from JWT: {branchId}");
                }
                else
                {
                    // Fallback: Lấy từ userId trong JWT và query database
                    // JWT sử dụng ClaimTypes.NameIdentifier cho userId (theo JwtService.cs)
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    Console.WriteLine($"[TypeComponentController.Create] UserId from JWT (ClaimTypes.NameIdentifier): {userIdClaim}");
                    
                    // Log tất cả claims để debug
                    Console.WriteLine($"[TypeComponentController.Create] All User Claims:");
                    foreach (var claim in User.Claims)
                    {
                        Console.WriteLine($"[TypeComponentController.Create]   {claim.Type} = {claim.Value}");
                    }
                    
                    if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var userId))
                    {
                        Console.WriteLine($"[TypeComponentController.Create] Parsed userId: {userId}");
                        
                        try
                        {
                            var user = await _userRepository.GetByIdAsync(userId);
                            if (user != null)
                            {
                                Console.WriteLine($"[TypeComponentController.Create] User found in database: Id={user.Id}, Username={user.Username}, BranchId={user.BranchId}");
                                
                                if (user.BranchId.HasValue)
                                {
                                    branchId = user.BranchId;
                                    Console.WriteLine($"[TypeComponentController.Create] ✅ Successfully got branchId from user profile: {branchId}");
                                }
                                else
                                {
                                    Console.WriteLine($"[TypeComponentController.Create] ❌ User {userId} ({user.Username}) does not have BranchId assigned in database");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[TypeComponentController.Create] ❌ Không tìm thấy người dùng trong database, Id: {userId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TypeComponentController.Create] ❌ Error querying user from database: {ex.Message}");
                            Console.WriteLine($"[TypeComponentController.Create] Stack trace: {ex.StackTrace}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[TypeComponentController.Create] ❌ Cannot parse userId from JWT claim. Value: '{userIdClaim}'");
                    }
                }

                // Nếu vẫn không có branchId, thử dùng từ request body (fallback cuối cùng)
                // Tương tự như VehicleCheckin - cho phép frontend gửi branchId
                if (!branchId.HasValue && dto.BranchId.HasValue)
                {
                    branchId = dto.BranchId;
                    Console.WriteLine($"[TypeComponentController.Create] ⚠️ Using branchId from request body (fallback): {branchId}");
                }

                // Nếu vẫn không có branchId sau tất cả các cách thử
                if (!branchId.HasValue)
                {
                    Console.WriteLine($"[TypeComponentController.Create] ❌ ERROR: Cannot determine branchId after all attempts");
                    Console.WriteLine($"[TypeComponentController.Create] RequestDto.BranchId: {dto.BranchId}");
                    
                    // Thông báo lỗi chi tiết
                    var errorMessage = "Không thể xác định chi nhánh của người dùng. ";
                    errorMessage += "Vui lòng liên hệ quản trị viên để được gán vào một chi nhánh trước khi tạo loại linh kiện.";
                    return BadRequest(new { success = false, message = errorMessage });
                }

                dto.BranchId = branchId;
                Console.WriteLine($"[TypeComponentController.Create] Creating TypeComponent with BranchId: {branchId}");
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, new { success = true, data = created, message = "TypeComponent created successfully" });
            }
            catch (ArgumentException ex)
            {
                // Lỗi nghiệp vụ (ví dụ linh kiện đã thuộc loại khác)
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TypeComponentController.Create] Exception: {ex.Message}");
                Console.WriteLine($"[TypeComponentController.Create] StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] RequestDto dto)
        {
            try
            {
                dto.Id = id;
                var updated = await _service.UpdateAsync(dto);
                if (updated == null) return NotFound(new { success = false, message = "TypeComponent not found" });
                return Ok(new { success = true, data = updated, message = "TypeComponent updated successfully" });
            }
            catch (ArgumentException ex)
            {
                // Lỗi nghiệp vụ (ví dụ linh kiện đã thuộc loại khác)
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
