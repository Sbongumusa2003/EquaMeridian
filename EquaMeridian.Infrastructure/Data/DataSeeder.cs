using EquaMeridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquaMeridian.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
        var admins = new[]
        {
            new { FullName = "Super Admin",   Email = "admin@equameridian.co.za",  Password = "Admin@1234!" },
            new { FullName = "System Admin",  Email = "sysadmin@equameridian.co.za", Password = "Sysadmin@1234!" }
        };

        foreach (var a in admins)
        {
            bool exists = await db.Users.AnyAsync(u => u.Email == a.Email);
            if (!exists)
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
    }
}