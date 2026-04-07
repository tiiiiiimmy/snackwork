using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Gamification;

namespace SnackSpot.Api.Services;

public class GamificationService(SnackSpotDbContext db) : IGamificationService
{
    public async Task AwardXpAsync(Guid userId, int amount)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null || user.IsDeleted) return;

        user.ExperiencePoints += amount;

        var newLevel = await db.UserLevels
            .Where(l => l.MinExperience <= user.ExperiencePoints)
            .OrderByDescending(l => l.Level)
            .Select(l => l.Level)
            .FirstOrDefaultAsync();

        user.Level = newLevel > 0 ? newLevel : 1;
        await db.SaveChangesAsync();
    }

    public async Task<UserLevelInfoResponse> GetUserLevelInfoAsync(Guid userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        var currentLevelConfig = await db.UserLevels.FindAsync(user.Level)
            ?? throw new InvalidOperationException("Level config not found");

        var nextLevelConfig = await db.UserLevels.FindAsync(user.Level + 1);

        return new UserLevelInfoResponse
        {
            Level = user.Level,
            Title = currentLevelConfig.Title,
            Description = currentLevelConfig.Description,
            ExperiencePoints = user.ExperiencePoints,
            MinExperience = currentLevelConfig.MinExperience,
            MaxExperience = currentLevelConfig.MaxExperience,
            NextLevelMinExperience = nextLevelConfig?.MinExperience,
            ExperienceToNextLevel = nextLevelConfig is not null
                ? nextLevelConfig.MinExperience - user.ExperiencePoints
                : null
        };
    }

    public async Task<UnlockedFeaturesResponse> GetUnlockedFeaturesAsync(Guid userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        var levels = await db.UserLevels
            .Where(l => l.Level <= user.Level && l.UnlockedFeatures != null)
            .ToListAsync();

        var features = levels
            .SelectMany(l => JsonSerializer.Deserialize<List<string>>(l.UnlockedFeatures!)
                             ?? [])
            .Where(f => f != "all")
            .Distinct()
            .OrderBy(f => f)
            .ToList();

        return new UnlockedFeaturesResponse
        {
            Level = user.Level,
            Features = features
        };
    }

    public async Task<IReadOnlyList<LevelConfigResponse>> GetAllLevelConfigsAsync()
    {
        var levels = await db.UserLevels
            .OrderBy(l => l.Level)
            .ToListAsync();

        return levels.Select(l => new LevelConfigResponse
        {
            Level = l.Level,
            MinExperience = l.MinExperience,
            MaxExperience = l.MaxExperience,
            Title = l.Title,
            Description = l.Description,
            UnlockedFeatures = l.UnlockedFeatures
        }).ToList();
    }
}
