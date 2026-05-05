using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private static readonly List<object> _categories = new()
    {
        new { id = 1, name = "Excavators" },
        new { id = 2, name = "Cranes" },
        new { id = 3, name = "Loaders" },
        new { id = 4, name = "Graders" },
        new { id = 5, name = "Compactors" },
        new { id = 6, name = "Trucks" },
    };

    [HttpGet]
    public IActionResult GetAll() => Ok(_categories);
}
