using Microsoft.AspNetCore.Mvc;

namespace KN.SafeCommunicationPlatform.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Hello";
            return View();
        }
    }
}
