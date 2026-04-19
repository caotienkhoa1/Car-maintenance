using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FE.controllers
{
    [Route("ServiceTasks")]
    public class ServiceTaskController : Controller
    {
        private readonly IConfiguration _configuration;

        public ServiceTaskController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
			return View("~/views/ServiceTasks/Index.cshtml");
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
			return View("~/views/ServiceTasks/Create.cshtml");
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            ViewBag.ServiceTaskId = id;
			return View("~/views/ServiceTasks/Edit.cshtml");
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            ViewBag.ServiceTaskId = id;
			return View("~/views/ServiceTasks/Details.cshtml");
        }
    }
}
