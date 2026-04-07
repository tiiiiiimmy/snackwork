using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Gamification;

namespace SnackSpot.Api.Services;

public class LeaderboardService(SnackSpotDbContext db, IDistributedCache cache) : ILeaderboardService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string[] ValidTypes = ["experience", "contribution", "activity", "influence"];

    public async Task<LeaderboardResponse> GetLeaderboardAsync(string type, int limit)
    {
        if (!ValidTypes.Contains(type))
            throw new ArgumentException($"Invalid leaderboard type '{type}'. Valid types: {string.Join(", ", ValidTypes)}");

        limit = Math.Min(limit, 100);
        var cacheKey = $"lb:{type}:{limit}";

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            var deserialized = JsonSerializer.Deserialize<LeaderboardResponse>(cached, JsonOptions);
            if (deserialized is not null) return deserialized;
        }

        var entries = await ComputeEntriesAsync(type, limit);
        var result = new LeaderboardResponse
        {
            Type = type,
            Entries = entries,
            ComputedAt = DateTime.UtcNow
        };

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

        return result;
    }

    public async Task<MyRankResponse> GetMyRankAsync(Guid userId, string type)
    {
        if (!ValidTypes.Contains(type))
            throw new ArgumentException($"Invalid leaderboard type '{type}'");

        var cacheKey = $"lb:{type}:all";
        List<LeaderboardEntryResponse> allEntries;

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            allEntries = JsonSerializer.Deserialize<List<LeaderboardEntryResponse>>(cached, JsonOptions) ?? [];
        }
        else
        {
            allEntries = (await ComputeEntriesAsync(type, int.MaxValue)).ToList();
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(allEntries, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
        }

        var entry = allEntries.FirstOrDefault(e => e.UserId == userId);
        return new MyRankResponse
        {
            Type = type,
            Rank = entry?.Rank,
            Score = entry?.Score ?? await ComputeUserScoreAsync(userId, type)
        };
    }

    // -- helpers --

    private async Task<IReadOnlyList<LeaderboardEntryResponse>> ComputeEntriesAsync(string type, int limit)
    {
        var users = await db.Users.Where(u => !u.IsDeleted).ToListAsync();

        var scored = new List<(Guid Id, string Username, string? AvatarUrl, int Level, int Score)>();

        foreach (var user in users)
        {
            var score = await ComputeUserScoreAsync(user.Id, type);
            scored.Add((user.Id, user.Username, user.AvatarUrl, user.Level, score));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Username)
            .Take(limit == int.MaxValue ? scored.Count : limit)
            .Select((x, index) => new LeaderboardEntryResponse
            {
                Rank = index + 1,
                UserId = x.Id,
                Username = x.Username,
                AvatarUrl = x.AvatarUrl,
                Level = x.Level,
                Score = x.Score
            })
            .ToList();
    }

    private async Task<int> ComputeUserScoreAsync(Guid userId, string type)
    {
        return type switch
        {
            "experience" => await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.ExperiencePoints)
                .FirstOrDefaultAsync(),

            "contribution" => await db.Snacks.CountAsync(s => s.CreatedByUserId == userId && !s.IsDeleted)
                            + await db.Reviews.CountAsync(r => r.UserId == userId && !r.IsDeleted),

            "activity" => await ComputeActivityScoreAsync(userId),

            "influence" => await db.Follows.CountAsync(f => f.FollowingId == userId)
                         + await db.Reviews
                               .Where(r => r.UserId == userId && !r.IsDeleted)
                               .SumAsync(r => r.LikeCount),

            _ => 0
        };
    }

    private async Task<int> ComputeActivityScoreAsync(Guid userId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var recentSnacks = await db.Snacks
            .CountAsync(s => s.CreatedByUserId == userId && !s.IsDeleted && s.CreatedAt >= cutoff);
        var recentReviews = await db.Reviews
            .CountAsync(r => r.UserId == userId && !r.IsDeleted && r.CreatedAt >= cutoff);
        return recentSnacks + recentReviews;
    }
}
