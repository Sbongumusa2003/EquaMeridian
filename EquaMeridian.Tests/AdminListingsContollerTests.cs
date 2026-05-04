using EquaMeridian.DTOs.Listings;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

public class AdminListingsControllerTests
{
    private readonly Mock<IListingRepository> _mockRepo = new();
    private readonly Mock<IAuditService> _mockAudit = new();
    private readonly Mock<IEmailService> _mockEmail = new();

    private AdminListingsController CreateController()
        => new AdminListingsController(_mockRepo.Object, _mockAudit.Object, _mockEmail.Object);
    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1))
                 .ReturnsAsync(new ListingDto { ListingID = 1, ListingTitle = "CAT 320" });

        var result = await CreateController().GetById(1);

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        var dto = ok!.Value as ListingDto;
        dto!.ListingID.Should().Be(1);
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ListingDto?)null);

        var result = await CreateController().GetById(99);

        result.Should().BeOfType<NotFoundResult>();
    }
    [Fact]
    public async Task UpdateStatus_Suspended_WithoutReason_ReturnsBadRequest()
    {
        var controller = CreateController();
        controller.ControllerContext = FakeAdminContext(1);

        var result = await controller.UpdateStatus(1,
            new UpdateListingStatusDto { NewStatus = "Suspended" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_ListingNotFound_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync((ListingDto?)null);
        var controller = CreateController();
        controller.ControllerContext = FakeAdminContext(1);

        var result = await controller.UpdateStatus(55,
            new UpdateListingStatusDto { NewStatus = "Active" });

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
