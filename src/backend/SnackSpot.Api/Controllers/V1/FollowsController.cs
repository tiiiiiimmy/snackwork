using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/users")]
public class FollowsController(IFollowService followService) : ControllerBase
{
    [HttpPost("{userId:guid}/follow")]
    [Authorize]
    public async Task<IActionResult> FollowUser(Guid userId)
    {
        var requestingUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await followService.FollowAsync(requestingUserId, userId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpDelete("{userId:guid}/follow")]
    [Authorize]
    public async Task<IActionResult> UnfollowUser(Guid userId)
    {
        var requestingUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await followService.UnfollowAsync(requestingUserId, userId);
        return Ok(ApiResponse.Ok("Unfollowed successfully"));
    }

    [HttpGet("{userId:guid}/following")]
    public async Task<IActionResult> GetFollowing(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var requestingUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? requestingUserId = requestingUserIdStr is not null ? Guid.Parse(requestingUserIdStr) : null;
        var result = await followService.GetFollowingAsync(userId, page, pageSize, requestingUserId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{userId:guid}/followers")]
    public async Task<IActionResult> GetFollowers(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var requestingUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? requestingUserId = requestingUserIdStr is not null ? Guid.Parse(requestingUserIdStr) : null;
        var result = await followService.GetFollowersAsync(userId, page, pageSize, requestingUserId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{userId:guid}/follow-status")]
    [Authorize]
    public async Task<IActionResult> GetFollowStatus(Guid userId)
    {
        var requestingUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await followService.GetFollowStatusAsync(requestingUserId, userId);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
