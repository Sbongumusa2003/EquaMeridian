using Microsoft.AspNetCore.Mvc;

namespace EquaMeridian.Controllers
{
    public class SupplierListingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
