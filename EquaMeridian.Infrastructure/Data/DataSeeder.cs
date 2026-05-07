using EquaMeridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EquaMeridian.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration? config = null)
    {
        await db.Database.MigrateAsync();

        var admins = new[]
        {
            new
            {
                FullName = "Super Admin",
                Email    = config?["Seed:AdminEmail"]    ?? "admin@equameridian.co.za",
                Password = config?["Seed:AdminPassword"] ?? GenerateFallbackPassword("admin")
            },
            new
            {
                FullName = "System Admin",
                Email    = config?["Seed:SysAdminEmail"]    ?? "sysadmin@equameridian.co.za",
                Password = config?["Seed:SysAdminPassword"] ?? GenerateFallbackPassword("sysadmin")
            }
        };

        foreach (var a in admins)
        {
            if (!await db.Users.AnyAsync(u => u.Email == a.Email))
            {
                db.Users.Add(new User
                {
                    FullName = a.FullName,
                    Email = a.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(a.Password),
                    Role = "admin",
                    AccountStatus = "Active",
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();

        var defaultCategories = new[]
        {
            "Excavators", "Cranes", "Loaders", "Graders", "Compactors", "Trucks"
        };

        foreach (var name in defaultCategories)
        {
            if (!await db.Categories.AnyAsync(c => c.Name == name))
            {
                db.Categories.Add(new Category { Name = name });
            }
        }

        await db.SaveChangesAsync();
    }

    private static string GenerateFallbackPassword(string prefix)
    {
        var random = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        Console.WriteLine($"[SEED WARNING] No password configured for '{prefix}' admin. " +
                          "A random password was generated. Use forgot-password to set a real one.");
        return random;
    }
}