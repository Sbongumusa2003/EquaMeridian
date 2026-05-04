using EquaMeridian.DTOs.User;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

public class UsersControllerTests
{
    private readonly Mock<IUserRepository> _mockRepo = new();
    private readonly Mock<IAuditService> _mockAudit = new();
    private readonly Mock<IEmailService> _mockEmail = new();

    private UsersController CreateController()
        => new UsersController(_mockRepo.Object, _mockAudit.Object, _mockEmail.Object);
    [Fact]
    public async Task GetAll_ReturnsOk_WithUserList()
    {
        var users = new List<UserDto>
        {
            new UserDto { UserID = 1, Email = "admin@test.com", Role = "admin" },
            new UserDto { UserID = 2, Email = "sup@test.com",   Role = "Supplier" }
        };
        _mockRepo.Setup(r => r.GetAllAsync(null, null, null, 1, 10))
                 .ReturnsAsync((users, 2));
        _mockAudit.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController();
        controller.ControllerContext = FakeAdminContext(1);
        var result = await controller.GetAll(null, null, null, 1, 10);
        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_WithSearch_FiltersCorrectly()
    {
        var filtered = new List<UserDto>
            { new UserDto { UserID = 2, Email = "john@test.com", FullName = "John" } };

        _mockRepo.Setup(r => r.GetAllAsync("john", null, null, 1, 10))
                 .ReturnsAsync((filtered, 1));
        _mockAudit.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController();
        controller.ControllerContext = FakeAdminContext(1);

        var result = await controller.GetAll("john", null, null, 1, 10);

        result.Should().BeOfType<OkObjectResult>();
    }
    [Fact]
    public async Task UpdateStatus_UserNotFound_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
        var controller = CreateController();
        controller.ControllerContext = FakeAdminContext(1);

        var result = await controller.UpdateStatus(99,
            new UpdateAccountStatusDto { NewStatus = "Active" });

        result.Should().BeOfType<NotFoundResult>();
    }
    private static ControllerContext FakeAdminContext(int adminId)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                adminId.ToString()),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Role, "admin")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            { User = principal }
        };
    }
}
