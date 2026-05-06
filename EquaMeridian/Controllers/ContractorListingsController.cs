using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/contractor/listings")]
[Authorize]
public class ContractorListingsController : ControllerBase
{
    private readonly IListingRepository _repo;
    public ContractorListingsController(IListingRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var (listings, total) = await _repo.GetAllAsync(
            search, category, "Active", page, pageSize);
        return Ok(new { listings, totalCount = total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var listing = await _repo.GetByIdAsync(id);
        return listing == null ? NotFound() : Ok(listing);
    }

    [HttpGet("compare")]
    public async Task<IActionResult> Compare([FromQuery] string ids)
    {
        var idList = ids.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .Distinct()
            .ToList();

        if (idList.Count < 2)
            return BadRequest(new { message = "Please select at least two listings to compare." });
        if (idList.Count > 4)
            return BadRequest(new { message = "Maximum 4 listings can be compared." });
        var listings = (await _repo.GetByIdsAsync(idList)).ToList();
        foreach (var id in idList)
        {
            var listing = listings.FirstOrDefault(l => l.ListingID == id);
            if (listing == null || listing.AvailabilityStatus != "Active")
                return NotFound(new { message = $"Listing {id} not found or not active." });
        }
        var ordered = idList.Select(id => listings.First(l => l.ListingID == id));
        return Ok(ordered);
    }
}