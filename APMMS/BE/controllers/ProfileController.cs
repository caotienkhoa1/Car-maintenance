using Microsoft.AspNetCore.Mvc;

namespace BE.controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
