using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Feed;

namespace SnackSpot.Api.Services;

public interface IFeedService
{
    Task<PagedResponse<RecommendationItem>> GetRecommendationsAsync(
        Guid userId, int page, int limit, decimal? lat, decimal? lng);
}
