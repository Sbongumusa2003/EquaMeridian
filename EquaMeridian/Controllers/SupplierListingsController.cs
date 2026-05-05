// REPLACE EquaMeridian/Controllers/SupplierListingsController.cs
using EquaMeridian.DTOs.Listings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/supplier/listings")]
[Authorize(Policy = "SupplierOnly")]
public class SupplierListingsController : ControllerBase
{
    private readonly IListingRepository _repo;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public SupplierListingsController(IListingRepository repo,
        IAuditService audit, IEmailService email, IConfiguration config)
    { _repo = repo; _audit = audit; _email = email; _config = config; }

    private int SupplierId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── UC 5.1: Create Listing ───────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateListingDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var listingId = await _repo.CreateAsync(dto, SupplierId);
        var snapshot = System.Text.Json.JsonSerializer.Serialize(dto);

        await _audit.LogAsync(SupplierId, "LISTING_CREATED",
            $"New listing created: {dto.ListingTitle}",
            null, listingId, null, ip, snapshot);

        var adminEmail = _config["AdminEmail"] ?? "admin@equameridian.co.za";
        await _email.SendNewListingPendingReviewAsync(
            adminEmail, listingId, User.FindFirstValue(ClaimTypes.Name) ?? "");

        // UC 5.1: Check duplicate flag and send additional admin notification
        var created = await _repo.GetByIdAsync(listingId);
        if (created?.DuplicateFlag == true)
        {
            await _email.SendDuplicateListingFlaggedAsync(
                adminEmail, listingId, dto.ListingTitle,
                User.FindFirstValue(ClaimTypes.Name) ?? "");
        }

        return CreatedAtAction(nameof(GetOwn),
            new { listingId },
            new { listingId, status = "Pending" });
    }

    // ─── UC 5.2: View Own Listings ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetOwn(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (listings, total) = await _repo.GetBySupplierAsync(
            SupplierId, status, page, pageSize);
        var enriched = listings.Select(l => new {
            listing = l,
            actions = GetAllowedActions(l.AvailabilityStatus)
        });

        return Ok(new { listings = enriched, totalCount = total, page, pageSize });
    }

    // ─── UC 5.3: Update Listing ───────────────────────────────────────────────
    [HttpPut("{listingId}")]
    public async Task<IActionResult> Update(
        int listingId, [FromBody] UpdateListingDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var listing = await _repo.GetByIdAsync(listingId);
        if (listing == null || listing.SupplierID != SupplierId) return NotFound();

        // UC 5.3: Suspended listings cannot be edited
        if (listing.AvailabilityStatus == "Suspended")
            return Forbid();

        var previous = System.Text.Json.JsonSerializer.Serialize(listing);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        // UC 5.3: Capture price before update to check for ≥20% change
        var originalDailyRate = listing.DailyRateZAR;

        await _repo.UpdateAsync(listingId, dto);

        var updated = await _repo.GetByIdAsync(listingId);
        var newSnap = System.Text.Json.JsonSerializer.Serialize(updated);

        await _audit.LogAsync(SupplierId, "LISTING_UPDATED",
            $"Listing {listingId} updated", null, listingId, previous, ip, newSnap);

        // UC 5.3: If price change ≥20%, notify admin that re-approval is required
        if (originalDailyRate > 0)
        {
            var priceChange = Math.Abs((dto.DailyRateZAR - originalDailyRate) / originalDailyRate);
            if (priceChange >= 0.20m)
            {
                var adminEmail = _config["AdminEmail"] ?? "admin@equameridian.co.za";
                var supplierName = User.FindFirstValue(ClaimTypes.Name) ?? "";
                await _email.SendListingPriceReApprovalRequiredAsync(
                    adminEmail, listingId, listing.ListingTitle,
                    supplierName, originalDailyRate, dto.DailyRateZAR);
            }
        }

        return Ok(updated);
    }

    // ─── UC 5.4: Deactivate Listing ──────────────────────────────────────────
    [HttpPatch("{listingId}/deactivate")]
    public async Task<IActionResult> Deactivate(int listingId)
    {
        var listing = await _repo.GetByIdAsync(listingId);
        if (listing == null || listing.SupplierID != SupplierId) return NotFound();

        if (listing.AvailabilityStatus != "Active")
            return BadRequest(new { message = "Only Active listings can be deactivated." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _repo.DeactivateAsync(listingId);

        await _audit.LogAsync(SupplierId, "LISTING_DEACTIVATED",
            $"Listing {listingId} deactivated",
            null, listingId, "Active", ip, "Inactive");

        return Ok(new { listingId, status = "Inactive" });
    }

    private static object GetAllowedActions(string status) => status switch
    {
        "Active" => new { canEdit = true, canDeactivate = true, contactAdmin = false },
        "Pending" => new { canEdit = true, canDeactivate = false, contactAdmin = false },
        "Suspended" => new { canEdit = false, canDeactivate = false, contactAdmin = true },
        "Inactive" => new { canEdit = true, canDeactivate = false, contactAdmin = false },
        _ => new { canEdit = false, canDeactivate = false, contactAdmin = false }
    };
}