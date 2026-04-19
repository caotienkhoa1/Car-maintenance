using Microsoft.AspNetCore.Mvc;

namespace BE.controllers
{
    public class HistoryLogController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
