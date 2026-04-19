using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FE.controllers
{
    [Route("TotalReceipts")]
    public class TotalReceiptController : Controller
    {
        private readonly IConfiguration _configuration;

        public TotalReceiptController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View("~/views/TotalReceipts/Index.cshtml");
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(long id)
        {
            ViewBag.TotalReceiptId = id;
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7173/api";
            return View("~/views/TotalReceipts/Details.cshtml");
        }
    }
}


