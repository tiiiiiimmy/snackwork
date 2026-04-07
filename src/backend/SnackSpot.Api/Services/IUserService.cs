using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Reviews;
using SnackSpot.Api.Models.DTOs.Snacks;
using SnackSpot.Api.Models.DTOs.Users;

namespace SnackSpot.Api.Services;

public interface IUserService
{
    Task<UserProfileResponse> GetUserProfileAsync(Guid userId);
    Task<UserProfileResponse> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest req, Guid requestingUserId);
    Task<PagedResponse<SnackResponse>> GetUserSnacksAsync(Guid userId, int page, int pageSize);
    Task<PagedResponse<ReviewResponse>> GetUserReviewsAsync(Guid userId, int page, int pageSize);
}
