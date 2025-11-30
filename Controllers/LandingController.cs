using Microsoft.AspNetCore.Mvc;

namespace PayRollManagementSystem.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
