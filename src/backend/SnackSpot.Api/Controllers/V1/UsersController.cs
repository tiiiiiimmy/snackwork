using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Users;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/users")]
public class UsersController(IUserService userService, IGamificationService gamificationService) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var result = await userService.GetUserProfileAsync(userId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateUserProfile(Guid userId, [FromBody] UpdateUserProfileRequest req)
    {
        var requestingUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await userService.UpdateUserProfileAsync(userId, req, requestingUserId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{userId:guid}/snacks")]
    public async Task<IActionResult> GetUserSnacks(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await userService.GetUserSnacksAsync(userId, page, pageSize);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{userId:guid}/reviews")]
    public async Task<IActionResult> GetUserReviews(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await userService.GetUserReviewsAsync(userId, page, pageSize);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{userId:guid}/level")]
    public async Task<IActionResult> GetUserLevel(Guid userId)
    {
        var result = await gamificationService.GetUserLevelInfoAsync(userId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{userId:guid}/unlocked-features")]
    public async Task<IActionResult> GetUnlockedFeatures(Guid userId)
    {
        var result = await gamificationService.GetUnlockedFeaturesAsync(userId);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
