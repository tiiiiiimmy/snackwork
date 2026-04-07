using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Snacks;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/snacks")]
public class SnacksController(ISnackService snackService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSnacks(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? storeId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        var result = await snackService.GetSnacksAsync(categoryId, storeId, search, page, pageSize);
        return Ok(ApiResponse<PagedResponse<SnackResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSnack(Guid id)
    {
        var result = await snackService.GetSnackAsync(id);
        return Ok(ApiResponse<SnackResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSnack([FromBody] CreateSnackRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await snackService.CreateSnackAsync(request, userId);
        return StatusCode(201, ApiResponse<SnackResponse>.Ok(result, "Snack created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateSnack(Guid id, [FromBody] UpdateSnackRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await snackService.UpdateSnackAsync(id, request, userId);
        return Ok(ApiResponse<SnackResponse>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteSnack(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await snackService.DeleteSnackAsync(id, userId);
        return Ok(ApiResponse.Ok("Snack deleted."));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchSnacks(
        [FromQuery] string? q,
        [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] decimal? minRating,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var result = await snackService.SearchSnacksAsync(q, categoryId, minPrice, maxPrice, minRating, sort, page, limit);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
