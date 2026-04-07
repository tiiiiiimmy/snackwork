using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/feed")]
public class FeedController(IFeedService feedService) : ControllerBase
{
    [HttpGet("recommendations")]
    [Authorize]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] decimal? lat = null,
        [FromQuery] decimal? lng = null)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await feedService.GetRecommendationsAsync(userId, page, limit, lat, lng);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
