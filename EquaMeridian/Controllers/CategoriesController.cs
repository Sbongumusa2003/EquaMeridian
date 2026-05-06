using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db) => _db = db;
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new { c.CategoryID, c.Name })
            .ToListAsync();

        return Ok(categories);
    }
}

// NOTE: You will also need:
//
// 1. Add a Category entity to EquaMeridian.Core/Entities/Category.cs:
//
//    public class Category
//    {
//        public int CategoryID { get; set; }
//        public string Name { get; set; } = string.Empty;
//    }
//
// 2. Add to AppDbContext:
//    public DbSet<Category> Categories => Set<Category>();
//
// 3. Add a migration:
//    dotnet ef migrations add AddCategoriesTable --project EquaMeridian.Infrastructure
//
// 4. Seed categories in DataSeeder.SeedAsync():
//    var defaultCategories = new[] { "Excavators", "Cranes", "Loaders", "Graders", "Compactors", "Trucks" };
//    foreach (var name in defaultCategories)
//    {
//        if (!await db.Categories.AnyAsync(c => c.Name == name))
//            db.Categories.Add(new Category { Name = name });
//    }
//    await db.SaveChangesAsync();
//
// 5. Update CreateListingDto CategoryID validation to verify the ID exists.