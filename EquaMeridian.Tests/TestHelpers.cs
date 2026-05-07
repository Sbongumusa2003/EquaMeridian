using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public static class TestHelpers
{
    // ─── Fake HTTP contexts ───────────────────────────────────────────────────

    public static ControllerContext FakeAdminContext(int adminId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
            new Claim(ClaimTypes.Role, "admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    public static ControllerContext FakeSupplierContext(int supplierId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, supplierId.ToString()),
            new Claim(ClaimTypes.Role, "Supplier")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ─── In-memory DbContext ──────────────────────────────────────────────────
    // Used by tests that need to construct controllers which require AppDbContext.
    // Each call returns a fresh, isolated database so tests do not share state.

    public static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}