using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Common;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Reviews;
using SnackSpot.Api.Models.DTOs.Snacks;
using SnackSpot.Api.Models.DTOs.Users;

namespace SnackSpot.Api.Services;

public class UserService(SnackSpotDbContext db) : IUserService
{
    public async Task<UserProfileResponse> GetUserProfileAsync(Guid userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        return await BuildProfileResponseAsync(user);
    }

    public async Task<UserProfileResponse> UpdateUserProfileAsync(
        Guid userId, UpdateUserProfileRequest req, Guid requestingUserId)
    {
        if (userId != requestingUserId)
            throw new UnauthorizedAccessException("Cannot update another user's profile");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        if (req.AvatarUrl is not null) user.AvatarUrl = req.AvatarUrl;
        if (req.Bio is not null) user.Bio = req.Bio;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return await BuildProfileResponseAsync(user);
    }

    public async Task<PagedResponse<SnackResponse>> GetUserSnacksAsync(Guid userId, int page, int pageSize)
    {
        _ = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        var query = db.Snacks
            .Include(s => s.Category)
            .Include(s => s.Store)
            .Include(s => s.Images)
            .Include(s => s.Tags)
            .Where(s => s.CreatedByUserId == userId && !s.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResponse<SnackResponse>.Create(
            items.Select(s => new SnackResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CategoryId = s.CategoryId,
                CategoryName = s.Category?.Name ?? string.Empty,
                StoreId = s.StoreId,
                StoreName = s.Store?.Name ?? string.Empty,
                CreatedByUserId = s.CreatedByUserId,
                Price = s.Price,
                AverageRating = s.AverageRating,
                TotalRatings = s.TotalRatings,
                TotalReviews = s.TotalReviews,
                Images = s.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ImageResponse { Id = i.Id, ImageUrl = i.ImageUrl, DisplayOrder = i.DisplayOrder })
                    .ToList(),
                Tags = s.Tags.Select(t => t.TagName).ToList(),
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList(),
            page, pageSize, total);
    }

    public async Task<PagedResponse<ReviewResponse>> GetUserReviewsAsync(Guid userId, int page, int pageSize)
    {
        _ = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        var query = db.Reviews
            .Include(r => r.User)
            .Include(r => r.Snack)
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResponse<ReviewResponse>.Create(
            items.Select(r => new ReviewResponse
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
            }).ToList(),
            page, pageSize, total);
    }

    // -- helpers --

    private async Task<UserProfileResponse> BuildProfileResponseAsync(Models.Entities.User user)
    {
        var snacksCount = await db.Snacks.CountAsync(s => s.CreatedByUserId == user.Id && !s.IsDeleted);
        var reviewsCount = await db.Reviews.CountAsync(r => r.UserId == user.Id && !r.IsDeleted);
        var followersCount = await db.Follows.CountAsync(f => f.FollowingId == user.Id);
        var followingCount = await db.Follows.CountAsync(f => f.FollowerId == user.Id);

        return new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Level = user.Level,
            ExperiencePoints = user.ExperiencePoints,
            SnacksCount = snacksCount,
            ReviewsCount = reviewsCount,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            CreatedAt = user.CreatedAt
        };
    }
}
