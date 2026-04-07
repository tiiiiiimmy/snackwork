using SnackSpot.Api.Models.DTOs.Gamification;

namespace SnackSpot.Api.Services;

public interface IGamificationService
{
    Task AwardXpAsync(Guid userId, int amount);
    Task<UserLevelInfoResponse> GetUserLevelInfoAsync(Guid userId);
    Task<UnlockedFeaturesResponse> GetUnlockedFeaturesAsync(Guid userId);
    Task<IReadOnlyList<LevelConfigResponse>> GetAllLevelConfigsAsync();
}
