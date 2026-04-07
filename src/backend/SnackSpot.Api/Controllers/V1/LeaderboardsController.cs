using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/leaderboards")]
public class LeaderboardsController(ILeaderboardService leaderboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string type = "experience",
        [FromQuery] int limit = 20)
    {
        var result = await leaderboardService.GetLeaderboardAsync(type, limit);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("my-rank")]
    [Authorize]
    public async Task<IActionResult> GetMyRank([FromQuery] string type = "experience")
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await leaderboardService.GetMyRankAsync(userId, type);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
