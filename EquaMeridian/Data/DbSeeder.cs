using EquaMeridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seeds mandatory dev/test users into the database on application startup.
/// Only runs when the environment is Development and the user does not already exist.
/// </summary>
public static class DbSeeder
{
    private static readonly (string FullName, string Email, string Password,
                               string Role, string? Company)[] SeedUsers =
    {
        // Role must match casing expected by the ASP.NET Core policies:
        //   "admin"      → AdminOnly    policy (RequireRole("admin"))
        //   "Supplier"   → SupplierOnly policy (RequireRole("Supplier"))
        //   "contractor" → no policy, uses plain [Authorize]
        ("System Admin",    "admin@equameridian.co.za",      "Admin@1234",      "admin",      null),
        ("Test Supplier",   "supplier@equameridian.co.za",   "Supplier@1234",   "Supplier",   "Test Supply Co"),
        ("Test Contractor", "contractor@equameridian.co.za", "Contractor@1234", "contractor", "Test Build Co"),
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        if (!env.IsDevelopment()) return;

        // Apply any pending migrations automatically in dev
        await db.Database.MigrateAsync();

        foreach (var (fullName, email, password, role, company) in SeedUsers)
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (existing is null)
            {
                db.Users.Add(new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = role,
                    AccountStatus = "Active",
                    CompanyName = company,
                    CreatedDate = DateTime.UtcNow
                });
                Console.WriteLine($"[Seeder] Created user: {email}  password: {password}");
            }
            else
            {
                // Always ensure seed accounts stay Active in dev
                // (guards against accidentally locking yourself out)
                if (existing.AccountStatus != "Active" ||
                    existing.FailedAttemptCount > 0)
                {
                    existing.AccountStatus = "Active";
                    existing.FailedAttemptCount = 0;
                    existing.LockoutExpiry = null;
                    existing.Role = role; // fix casing if wrong
                    Console.WriteLine($"[Seeder] Reset user to Active: {email}");
                }
            }
        }

        await db.SaveChangesAsync();
    }
}