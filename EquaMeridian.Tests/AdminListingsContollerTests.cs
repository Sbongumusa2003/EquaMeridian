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
        => new(_mockRepo.Object, _mockAudit.Object, _mockEmail.Object);
    private void SetupAudit()
        => _mockAudit
            .Setup(a => a.LogAsync(
                It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1))
                 .ReturnsAsync(new ListingDto { ListingID = 1, ListingTitle = "CAT 320" });

        var result = await CreateController().GetById(1);

        result.Should().BeOfType<OkObjectResult>();
        var dto = (result as OkObjectResult)!.Value as ListingDto;
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
        controller.ControllerContext = TestHelpers.FakeAdminContext(1);

        var result = await controller.UpdateStatus(1,
            new UpdateListingStatusDto { NewStatus = "Suspended" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_ListingNotFound_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync((ListingDto?)null);
        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(1);

        var result = await controller.UpdateStatus(55,
            new UpdateListingStatusDto { NewStatus = "Active" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateStatus_Active_CallsRepoAndAuditAndEmail()
    {
        SetupAudit();
        _mockEmail
            .Setup(e => e.SendListingStatusChangedAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var existing = new ListingDto
        {
            ListingID = 10,
            SupplierID = 2,
            SupplierName = "ACME",
            AvailabilityStatus = "Pending"
        };
        _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateStatusAsync(10, "Active")).Returns(Task.CompletedTask);

        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(1);

        var result = await controller.UpdateStatus(10,
            new UpdateListingStatusDto { NewStatus = "Active" });

        result.Should().BeOfType<OkObjectResult>();
        _mockRepo.Verify(r => r.UpdateStatusAsync(10, "Active"), Times.Once);
        _mockAudit.Verify(a => a.LogAsync(
            2, "LISTING_STATUS_UPDATED", It.IsAny<string?>(),
            1, 10, "Pending", It.IsAny<string?>(), "Active"), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_Suspended_WithReason_Succeeds()
    {
        SetupAudit();
        _mockEmail
            .Setup(e => e.SendListingStatusChangedAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var existing = new ListingDto
        {
            ListingID = 5,
            SupplierID = 3,
            AvailabilityStatus = "Active"
        };
        _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateStatusAsync(5, "Suspended")).Returns(Task.CompletedTask);

        var controller = CreateController();
        controller.ControllerContext = TestHelpers.FakeAdminContext(1);

        var result = await controller.UpdateStatus(5,
            new UpdateListingStatusDto { NewStatus = "Suspended", SuspensionReason = "Fraud detected" });

        result.Should().BeOfType<OkObjectResult>();
    }
}