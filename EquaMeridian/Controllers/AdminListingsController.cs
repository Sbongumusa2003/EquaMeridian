using Microsoft.AspNetCore.Mvc;

namespace EquaMeridian.Controllers
{
    public class AdminListingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
