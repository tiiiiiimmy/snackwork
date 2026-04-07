using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Follow;

namespace SnackSpot.Api.Services;

public interface IFollowService
{
    Task<FollowStatusResponse> FollowAsync(Guid followerId, Guid followingId);
    Task UnfollowAsync(Guid followerId, Guid followingId);
    Task<PagedResponse<UserSummaryResponse>> GetFollowingAsync(Guid userId, int page, int pageSize, Guid? requestingUserId);
    Task<PagedResponse<UserSummaryResponse>> GetFollowersAsync(Guid userId, int page, int pageSize, Guid? requestingUserId);
    Task<FollowStatusResponse> GetFollowStatusAsync(Guid requestingUserId, Guid targetUserId);
}
