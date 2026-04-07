using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Reviews;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpGet("snacks/{snackId:guid}/reviews")]
    public async Task<IActionResult> GetReviews(
        Guid snackId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        var result = await reviewService.GetReviewsAsync(snackId, page, pageSize);
        return Ok(ApiResponse<PagedResponse<ReviewResponse>>.Ok(result));
    }

    [HttpPost("snacks/{snackId:guid}/reviews")]
    [Authorize]
    public async Task<IActionResult> CreateReview(Guid snackId, [FromBody] CreateReviewRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await reviewService.CreateReviewAsync(snackId, request, userId);
        return StatusCode(201, ApiResponse<ReviewResponse>.Ok(result, "Review created."));
    }

    [HttpGet("reviews/{id:guid}")]
    public async Task<IActionResult> GetReview(Guid id)
    {
        var result = await reviewService.GetReviewAsync(id);
        return Ok(ApiResponse<ReviewResponse>.Ok(result));
    }

    [HttpPut("reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await reviewService.UpdateReviewAsync(id, request, userId);
        return Ok(ApiResponse<ReviewResponse>.Ok(result));
    }

    [HttpDelete("reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await reviewService.DeleteReviewAsync(id, userId);
        return Ok(ApiResponse.Ok("Review deleted."));
    }

    [HttpPost("reviews/{id:guid}/like")]
    [Authorize]
    public async Task<IActionResult> ToggleLike(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await reviewService.ToggleLikeAsync(id, userId);
        return Ok(ApiResponse<LikeResponse>.Ok(result));
    }
}
