using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SnackSpot.Api.Common;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Feed;
using SnackSpot.Api.Models.DTOs.Snacks;

namespace SnackSpot.Api.Services;

public class FeedService(SnackSpotDbContext db, IDistributedCache cache) : IFeedService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<PagedResponse<RecommendationItem>> GetRecommendationsAsync(
        Guid userId, int page, int limit, decimal? lat, decimal? lng)
    {
        var cacheKey = $"recs:{userId}:{page}:{limit}";

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            var deserialized = JsonSerializer.Deserialize<PagedResponse<RecommendationItem>>(cached, JsonOptions);
            if (deserialized is not null) return deserialized;
        }

        var result = await ComputeRecommendationsAsync(userId, page, limit);

        var serialized = JsonSerializer.Serialize(result, JsonOptions);
        await cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        });

        return result;
    }

    private async Task<PagedResponse<RecommendationItem>> ComputeRecommendationsAsync(
        Guid userId, int page, int limit)
    {
        // User's preferred categories (top 3 by review count)
        var preferredCategoryIds = await db.Reviews
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .Include(r => r.Snack)
            .GroupBy(r => r.Snack.CategoryId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(3)
            .ToListAsync();

        // Snacks the user has already reviewed
        var reviewedSnackIds = await db.Reviews
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .Select(r => r.SnackId)
            .ToListAsync();

        // Candidates: not deleted, not reviewed by user
        var candidates = await db.Snacks
            .Where(s => !s.IsDeleted && !reviewedSnackIds.Contains(s.Id))
            .Include(s => s.Category)
            .Include(s => s.Store)
            .ToListAsync();

        if (candidates.Count == 0)
            return PagedResponse<RecommendationItem>.Create([], page, limit, 0);

        var maxReviews = Math.Max(candidates.Max(s => s.TotalReviews), 1);

        var scored = candidates.Select(s =>
        {
            var contentScore = preferredCategoryIds.Contains(s.CategoryId) ? 1.0m : 0.0m;
            var popularityScore =
                (s.AverageRating / 5.0m) * 0.5m +
                Math.Min((decimal)s.TotalReviews / maxReviews, 1.0m) * 0.5m;
            var finalScore = Math.Round(0.6m * contentScore + 0.4m * popularityScore, 4);
            var source = contentScore > 0 ? "content" : "popular";
            var reason = contentScore > 0 ? "基于您喜欢的分类" : "热门推荐";

            return new RecommendationItem
            {
                Snack = MapSnack(s),
                RecommendationReason = reason,
                Score = finalScore,
                Source = source
            };
        })
        .OrderByDescending(x => x.Score)
        .ThenByDescending(x => x.Snack.CreatedAt)
        .ToList();

        var total = scored.Count;
        var items = scored
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        return PagedResponse<RecommendationItem>.Create(items, page, limit, total);
    }

    private static SnackResponse MapSnack(Models.Entities.Snack s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        CategoryId = s.CategoryId,
        CategoryName = s.Category.Name,
        StoreId = s.StoreId,
        StoreName = s.Store.Name,
        CreatedByUserId = s.CreatedByUserId,
        Price = s.Price,
        AverageRating = s.AverageRating,
        TotalRatings = s.TotalRatings,
        TotalReviews = s.TotalReviews,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        Images = [],
        Tags = []
    };
}
