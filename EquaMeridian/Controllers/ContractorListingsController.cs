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
}