using EquaMeridian.DTOs.Listings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/admin/listings")]
[Authorize(Policy = "AdminOnly")]
public class AdminListingsController : ControllerBase
{
    private readonly IListingRepository _repo;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;

    public AdminListingsController(IListingRepository repo,
                                   IAuditService audit,
                                   IEmailService email)
    { _repo = repo; _audit = audit; _email = email; }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? category,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var (listings, total) = await _repo.GetAllAsync(search, category, status, page, pageSize);
        return Ok(new { listings, totalCount = total, page, pageSize });
    }

    [HttpGet("{listingId}")]
    public async Task<IActionResult> GetById(int listingId)
    {
        var listing = await _repo.GetByIdAsync(listingId);
        return listing == null ? NotFound() : Ok(listing);
    }

    [HttpPatch("{listingId}/status")]
    public async Task<IActionResult> UpdateStatus(
        int listingId, [FromBody] UpdateListingStatusDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (dto.NewStatus == "Suspended" && string.IsNullOrWhiteSpace(dto.SuspensionReason))
            return BadRequest(new { message = "Suspension reason is required." });

        var listing = await _repo.GetByIdAsync(listingId);
        if (listing == null) return NotFound();

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var previous = listing.AvailabilityStatus;

        await _repo.UpdateStatusAsync(listingId, dto.NewStatus);

        await _audit.LogAsync(listing.SupplierID, "LISTING_STATUS_UPDATED",
            dto.SuspensionReason ?? $"Status changed to {dto.NewStatus}",
            adminId, listingId, previous, ip, dto.NewStatus);

        await _email.SendListingStatusChangedAsync(
            "", listing.SupplierName, listingId, dto.NewStatus, dto.SuspensionReason);

        return Ok(new { listingId, newStatus = dto.NewStatus });
    }
}
