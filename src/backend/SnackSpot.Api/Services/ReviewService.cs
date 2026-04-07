using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Common;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Reviews;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Services;

public class ReviewService(SnackSpotDbContext db) : IReviewService
{
    public async Task<PagedResponse<ReviewResponse>> GetReviewsAsync(Guid snackId, int page, int pageSize)
    {
        var snackExists = await db.Snacks.AnyAsync(s => s.Id == snackId && !s.IsDeleted);
        if (!snackExists)
            throw new KeyNotFoundException($"Snack {snackId} not found.");

        var query = db.Reviews
            .Include(r => r.User)
            .Where(r => r.SnackId == snackId && !r.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResponse<ReviewResponse>.Create(
            items.Select(ToResponse).ToList(), page, pageSize, total);
    }

    public async Task<ReviewResponse> GetReviewAsync(Guid id)
    {
        var review = await db.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new KeyNotFoundException($"Review {id} not found.");
        return ToResponse(review);
    }

    public async Task<ReviewResponse> CreateReviewAsync(Guid snackId, CreateReviewRequest request, Guid userId)
    {
        var snack = await db.Snacks.FirstOrDefaultAsync(s => s.Id == snackId && !s.IsDeleted)
            ?? throw new KeyNotFoundException($"Snack {snackId} not found.");

        var alreadyReviewed = await db.Reviews.AnyAsync(r =>
            r.SnackId == snackId && r.UserId == userId && !r.IsDeleted);
        if (alreadyReviewed)
            throw new InvalidOperationException("You have already reviewed this snack.");

        var review = new Review
        {
            SnackId = snackId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        await RecalculateSnackStatsAsync(snackId);

        // Reload with user for response
        return await GetReviewAsync(review.Id);
    }

    public async Task<ReviewResponse> UpdateReviewAsync(Guid id, UpdateReviewRequest request, Guid userId)
    {
        var review = await db.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new KeyNotFoundException($"Review {id} not found.");

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("Not the owner of this review.");

        if (request.Rating.HasValue) review.Rating = request.Rating.Value;
        if (request.Comment is not null) review.Comment = request.Comment;
        review.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await RecalculateSnackStatsAsync(review.SnackId);

        return ToResponse(review);
    }

    public async Task DeleteReviewAsync(Guid id, Guid userId)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new KeyNotFoundException($"Review {id} not found.");

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("Not the owner of this review.");

        review.IsDeleted = true;
        await db.SaveChangesAsync();

        await RecalculateSnackStatsAsync(review.SnackId);
    }

    public async Task<LikeResponse> ToggleLikeAsync(Guid reviewId, Guid userId)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        var existing = await db.ReviewLikes
            .FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.UserId == userId);

        bool isLiked;
        if (existing is not null)
        {
            db.ReviewLikes.Remove(existing);
            review.LikeCount = Math.Max(0, review.LikeCount - 1);
            isLiked = false;
        }
        else
        {
            db.ReviewLikes.Add(new ReviewLike { ReviewId = reviewId, UserId = userId });
            review.LikeCount++;
            isLiked = true;
        }

        await db.SaveChangesAsync();
        return new LikeResponse { IsLiked = isLiked, LikeCount = review.LikeCount };
    }

    private async Task RecalculateSnackStatsAsync(Guid snackId)
    {
        var snack = await db.Snacks.FirstOrDefaultAsync(s => s.Id == snackId);
        if (snack is null) return;

        var activeReviews = await db.Reviews
            .Where(r => r.SnackId == snackId && !r.IsDeleted)
            .ToListAsync();

        snack.TotalReviews = activeReviews.Count;
        snack.TotalRatings = activeReviews.Count;
        snack.AverageRating = activeReviews.Count > 0
            ? Math.Round((decimal)activeReviews.Average(r => r.Rating), 2)
            : 0m;

        await db.SaveChangesAsync();
    }

    private static ReviewResponse ToResponse(Review r) => new()
    {
        Id = r.Id,
        SnackId = r.SnackId,
        UserId = r.UserId,
        Username = r.User?.Username ?? string.Empty,
        Rating = r.Rating,
        Comment = r.Comment,
        LikeCount = r.LikeCount,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
