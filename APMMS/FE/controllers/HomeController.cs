using Microsoft.AspNetCore.Mvc;

namespace FE.controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            ViewBag.CurrentUserId = HttpContext.Session.GetString("UserId");
            return View();
        }

        public IActionResult About()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            ViewBag.CurrentUserId = HttpContext.Session.GetString("UserId");
            return View();
        }

        public IActionResult Services()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            ViewBag.CurrentUserId = HttpContext.Session.GetString("UserId");
            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            ViewBag.CurrentUserId = HttpContext.Session.GetString("UserId");
            return View();
        }
    }
}
