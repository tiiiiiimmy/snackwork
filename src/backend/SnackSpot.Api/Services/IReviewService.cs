using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Reviews;

namespace SnackSpot.Api.Services;

public interface IReviewService
{
    Task<PagedResponse<ReviewResponse>> GetReviewsAsync(Guid snackId, int page, int pageSize);
    Task<ReviewResponse> GetReviewAsync(Guid id);
    Task<ReviewResponse> CreateReviewAsync(Guid snackId, CreateReviewRequest request, Guid userId);
    Task<ReviewResponse> UpdateReviewAsync(Guid id, UpdateReviewRequest request, Guid userId);
    Task DeleteReviewAsync(Guid id, Guid userId);
    Task<LikeResponse> ToggleLikeAsync(Guid reviewId, Guid userId);
}
