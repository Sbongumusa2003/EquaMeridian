using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public static class TestHelpers
{
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
}