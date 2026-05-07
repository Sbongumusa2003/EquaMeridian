using EquaMeridian.DTOs.Listings;
using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private readonly AppDbContext _db;
    private readonly IListingImageRepository _imageRepo;

    public SupplierListingsController(
        IListingRepository repo,
        IAuditService audit,
        IEmailService email,
        IConfiguration config,
        AppDbContext db,
        IListingImageRepository imageRepo)
    {
        _repo = repo;
        _audit = audit;
        _email = email;
        _config = config;
        _db = db;
        _imageRepo = imageRepo;
    }

    private int SupplierId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateListingDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var categoryExists = await _db.Categories
            .AnyAsync(c => c.CategoryID == dto.CategoryID);
        if (!categoryExists)
            return BadRequest(new { message = "Invalid category." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var listingId = await _repo.CreateAsync(dto, SupplierId);
        var snapshot = System.Text.Json.JsonSerializer.Serialize(dto);

        await _audit.LogAsync(SupplierId, "LISTING_CREATED",
            $"New listing created: {dto.ListingTitle}",
            null, listingId, null, ip, snapshot);

        var adminEmail = _config["AdminEmail"] ?? "admin@equameridian.co.za";
        await _email.SendNewListingPendingReviewAsync(
            adminEmail, listingId,
            User.FindFirstValue(ClaimTypes.Name) ?? "");

        return CreatedAtAction(nameof(GetOwn),
            new { listingId },
            new { listingId, status = "Pending" });
    }

    // ─── POST /api/supplier/listings/{listingId}/images ──────────────────────
    /// <summary>
    /// Upload images for a listing that already exists.
    /// Accepts multipart/form-data with one or more files under the key "files".
    /// </summary>
    [HttpPost("{listingId}/images")]
    public async Task<IActionResult> UploadImages(
        int listingId, [FromForm] IFormFileCollection files)
    {
        // Verify the listing belongs to this supplier
        var listing = await _repo.GetByIdAsync(listingId);
        if (listing == null || listing.SupplierID != SupplierId)
            return NotFound(new { message = "Listing not found." });

        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files were uploaded." });

        var saved = await _imageRepo.AddImagesAsync(listingId, files);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _audit.LogAsync(SupplierId, "LISTING_IMAGES_UPLOADED",
            $"{saved.Count()} image(s) added to listing {listingId}",
            null, listingId, null, ip);

        return Ok(new { listingId, imageUrls = saved });
    }

    // ─── DELETE /api/supplier/listings/{listingId}/images/{imageId} ──────────
    [HttpDelete("{listingId}/images/{imageId}")]
    public async Task<IActionResult> DeleteImage(int listingId, int imageId)
    {
        var listing = await _repo.GetByIdAsync(listingId);
        if (listing == null || listing.SupplierID != SupplierId)
            return NotFound(new { message = "Listing not found." });

        var deleted = await _imageRepo.DeleteAsync(listingId, imageId);
        if (!deleted)
            return NotFound(new { message = "Image not found." });

        return Ok(new { message = "Image deleted." });
    }

    // ─── GET /api/supplier/listings ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetOwn(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var (listings, total) = await _repo.GetBySupplierAsync(
            SupplierId, status, page, pageSize);

        var enriched = listings.Select(l => new {
            listing = l,
            actions = GetAllowedActions(l.AvailabilityStatus)
        });

        return Ok(new { listings = enriched, totalCount = total, page, pageSize });
    }

    // ─── PUT /api/supplier/listings/{listingId} ───────────────────────────────
    [HttpPut("{listingId}")]
    public async Task<IActionResult> Update(
        int listingId, [FromBody] UpdateListingDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var categoryExists = await _db.Categories
            .AnyAsync(c => c.CategoryID == dto.CategoryID);
        if (!categoryExists)
            return BadRequest(new { message = "Invalid category." });

        var listing = await _repo.GetByIdAsync(listingId);
        if (listing == null || listing.SupplierID != SupplierId) return NotFound();
        if (listing.AvailabilityStatus == "Suspended") return Forbid();

        var previous = System.Text.Json.JsonSerializer.Serialize(listing);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _repo.UpdateAsync(listingId, dto);

        var updated = await _repo.GetByIdAsync(listingId);
        var newSnap = System.Text.Json.JsonSerializer.Serialize(updated);

        await _audit.LogAsync(SupplierId, "LISTING_UPDATED",
            $"Listing {listingId} updated", null, listingId, previous, ip, newSnap);

        return Ok(updated);
    }

    // ─── PATCH /api/supplier/listings/{listingId}/deactivate ─────────────────
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

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static object GetAllowedActions(string status) => status switch
    {
        "Active" => new { canEdit = true, canDeactivate = true, contactAdmin = false },
        "Pending" => new { canEdit = true, canDeactivate = false, contactAdmin = false },
        "Suspended" => new { canEdit = false, canDeactivate = false, contactAdmin = true },
        "Inactive" => new { canEdit = true, canDeactivate = false, contactAdmin = false },
        _ => new { canEdit = false, canDeactivate = false, contactAdmin = false }
    };
}