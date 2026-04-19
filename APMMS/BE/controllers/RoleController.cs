using Microsoft.AspNetCore.Mvc;

namespace BE.controllers
{
    public class RoleController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
