using Microsoft.AspNetCore.Mvc;

namespace FE.controllers
{
    [Route("Profile")]
    public class ProfileController : Controller
    {
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            // Profile chỉ dành cho AutoOwner ở trang Home
            // Không dùng Dashboard layout
            return View();
        }
    }
}
