using Microsoft.AspNetCore.Mvc;

namespace BE.controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
