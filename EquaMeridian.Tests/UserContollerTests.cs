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
        => new(_mockRepo.Object, _mockAudit.Object, _mockEmail.Object);

    private void SetupAudit()
        => _mockAudit
            .Setup(a => a.LogAsync(
                It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

    [Fact]
    public async Task GetAll_ReturnsOk_WithUserList()
    {
        SetupAudit();
        var users = new List<UserDto>
        {
            new() { UserID = 1, Email = "admin@test.com", Role = "admin" },
            new() { UserID = 2, Email = "sup@test.com",   Role = "Supplier" }
        };
        _mockRepo.Setup(r => r.GetAllAsync(null, null, null, 1, 10))
                 .ReturnsAsync((users, 2));

        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(1);

        var result = await controller.GetAll(null, null, null, 1, 10);

        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_WithSearch_FiltersCorrectly()
    {
        SetupAudit();
        var filtered = new List<UserDto>
            { new() { UserID = 2, Email = "john@test.com", FullName = "John" } };

        _mockRepo.Setup(r => r.GetAllAsync("john", null, null, 1, 10))
                 .ReturnsAsync((filtered, 1));

        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(1);

        var result = await controller.GetAll("john", null, null, 1, 10);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_LogsAuditEvent()
    {
        SetupAudit();
        _mockRepo.Setup(r => r.GetAllAsync(null, null, null, 1, 10))
                 .ReturnsAsync((new List<UserDto>(), 0));

        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(99);

        await controller.GetAll(null, null, null, 1, 10);

        _mockAudit.Verify(a => a.LogAsync(
            99, "ADMIN_PANEL_ACCESS", It.IsAny<string?>(),
            99, null, null, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_UserNotFound_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(1);

        var result = await controller.UpdateStatus(99,
            new UpdateAccountStatusDto { NewStatus = "Active" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateStatus_ValidUser_ReturnsOk_AndNotifiesUser()
    {
        SetupAudit();
        _mockEmail
            .Setup(e => e.SendAccountStatusChangedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var user = new User
        {
            UserID = 5,
            Email = "supplier@test.com",
            FullName = "Test Supplier",
            AccountStatus = "Pending"
        };
        _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateStatusAsync(5, "Active")).Returns(Task.CompletedTask);

        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(1);

        var result = await controller.UpdateStatus(5,
            new UpdateAccountStatusDto { NewStatus = "Active" });

        result.Should().BeOfType<OkObjectResult>();
        _mockRepo.Verify(r => r.UpdateStatusAsync(5, "Active"), Times.Once);
        _mockEmail.Verify(e => e.SendAccountStatusChangedAsync(
            "supplier@test.com", "Test Supplier", "Active"), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_LogsAuditWithPreviousAndNewStatus()
    {
        SetupAudit();
        _mockEmail
            .Setup(e => e.SendAccountStatusChangedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var user = new User
        {
            UserID = 7,
            Email = "u@test.com",
            FullName = "U",
            AccountStatus = "Suspended"
        };
        _mockRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateStatusAsync(7, "Active")).Returns(Task.CompletedTask);

        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(2);

        await controller.UpdateStatus(7, new UpdateAccountStatusDto { NewStatus = "Active" });

        _mockAudit.Verify(a => a.LogAsync(
            7, "ACCOUNT_STATUS_UPDATED", It.IsAny<string?>(),
            2, null, "Suspended", It.IsAny<string?>(), "Active"), Times.Once);
    }
}