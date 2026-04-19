using Microsoft.AspNetCore.Mvc;
using FE.services;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace FE.controllers
{
    [Route("MaintenanceTickets")]
    public class MaintenanceTicketController : Controller
    {
        private readonly MaintenanceTicketService _service;
        private readonly IConfiguration _configuration;

        public MaintenanceTicketController(MaintenanceTicketService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }
        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
			return View("~/views/MaintenanceTickets/Index.cshtml");
        }

        [HttpGet]
        [Route("ListData")]
        public async Task<IActionResult> ListData(int page = 1, int pageSize = 10)
        {
            // Lấy branchId từ session hoặc JWT claims
            long? branchId = null;
            
            // Ưu tiên lấy từ session
            var sessionBranchId = HttpContext.Session.GetString("BranchId");
            if (!string.IsNullOrEmpty(sessionBranchId) && long.TryParse(sessionBranchId, out var parsedBranchId))
            {
                branchId = parsedBranchId;
                Console.WriteLine($"[FE MaintenanceTicketController] Got branchId from session: {branchId}");
            }
            else
            {
                // Nếu không có trong session, thử lấy từ JWT claims
                if (User.Identity?.IsAuthenticated == true)
                {
                    var branchIdClaim = User.FindFirst("BranchId")?.Value;
                    if (long.TryParse(branchIdClaim, out var claimBranchId))
                    {
                        branchId = claimBranchId;
                        // Lưu vào session để lần sau không cần parse lại
                        HttpContext.Session.SetString("BranchId", branchIdClaim);
                        Console.WriteLine($"[FE MaintenanceTicketController] Got branchId from JWT claim: {branchId}");
                    }
                    else
                    {
                        Console.WriteLine("[FE MaintenanceTicketController] No branchId in session or JWT claim, trying VehicleCheckinService API");
                        // Fallback: gọi API để lấy thông tin employee
                        try
                        {
                            var vehicleCheckinService = HttpContext.RequestServices.GetRequiredService<FE.services.VehicleCheckinService>();
                            var employeeResponse = await vehicleCheckinService.GetEmployeeInfoAsync();
                            if (employeeResponse != null)
                            {
                                // Response format: { success: true, data: { id, branchId, branchName } }
                                var successProperty = employeeResponse.GetType().GetProperty("success");
                                var dataProperty = employeeResponse.GetType().GetProperty("data");
                                
                                if (successProperty != null && dataProperty != null)
                                {
                                    var success = successProperty.GetValue(employeeResponse);
                                    var employeeData = dataProperty.GetValue(employeeResponse);
                                    
                                    if (success != null && success.ToString() == "True" && employeeData != null)
                                    {
                                        var branchIdProp = employeeData.GetType().GetProperty("branchId") 
                                            ?? employeeData.GetType().GetProperty("BranchId");
                                        if (branchIdProp != null)
                                        {
                                            var branchIdValue = branchIdProp.GetValue(employeeData);
                                            if (branchIdValue != null && long.TryParse(branchIdValue.ToString(), out var empBranchId))
                                            {
                                                branchId = empBranchId;
                                                // Lưu vào session để lần sau không cần gọi API
                                                HttpContext.Session.SetString("BranchId", branchId.Value.ToString());
                                                Console.WriteLine($"[FE MaintenanceTicketController] Got branchId from Employee/me API: {branchId}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[FE MaintenanceTicketController] Error getting branchId: {ex.Message}");
                        }
                    }
                }
            }
            
            Console.WriteLine($"[FE MaintenanceTicketController] Calling GetAllAsync with branchId: {branchId}");
            var data = await _service.GetAllAsync(page, pageSize, branchId);
            return Json(data);
        }

        [HttpPost]
        [Route("CreateFromCheckin")] 
        public async Task<IActionResult> CreateFromCheckin([FromBody] object payload)
        {
            var result = await _service.CreateFromCheckinAsync(payload);
            return Json(result);
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
			return View("~/views/MaintenanceTickets/Create.cshtml");
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            ViewBag.MaintenanceTicketId = id;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
			return View("~/views/MaintenanceTickets/Details.cshtml");
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            ViewBag.MaintenanceTicketId = id;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
			return View("~/views/MaintenanceTickets/Details.cshtml");
        }
    }
}
