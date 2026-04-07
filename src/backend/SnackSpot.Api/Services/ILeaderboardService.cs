using SnackSpot.Api.Models.DTOs.Gamification;

namespace SnackSpot.Api.Services;

public interface ILeaderboardService
{
    Task<LeaderboardResponse> GetLeaderboardAsync(string type, int limit);
    Task<MyRankResponse> GetMyRankAsync(Guid userId, string type);
}
