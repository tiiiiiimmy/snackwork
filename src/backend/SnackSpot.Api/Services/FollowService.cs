using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Common;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Follow;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Services;

public class FollowService(SnackSpotDbContext db) : IFollowService
{
    public async Task<FollowStatusResponse> FollowAsync(Guid followerId, Guid followingId)
    {
        if (followerId == followingId)
            throw new InvalidOperationException("Cannot follow yourself");

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == followingId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        var existing = await db.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        if (existing is not null)
            throw new InvalidOperationException("Already following this user");

        db.Follows.Add(new Follow { FollowerId = followerId, FollowingId = followingId });
        await db.SaveChangesAsync();

        return await BuildFollowStatusAsync(followerId, followingId);
    }

    public async Task UnfollowAsync(Guid followerId, Guid followingId)
    {
        var follow = await db.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId)
            ?? throw new KeyNotFoundException("Not following this user");

        db.Follows.Remove(follow);
        await db.SaveChangesAsync();
    }

    public async Task<PagedResponse<UserSummaryResponse>> GetFollowingAsync(
        Guid userId, int page, int pageSize, Guid? requestingUserId)
    {
        var query = db.Follows
            .Where(f => f.FollowerId == userId)
            .Include(f => f.Following);

        var total = await query.CountAsync();
        var follows = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        HashSet<Guid> requestingFollowingIds = requestingUserId.HasValue
            ? [.. await db.Follows
                .Where(f => f.FollowerId == requestingUserId.Value)
                .Select(f => f.FollowingId)
                .ToListAsync()]
            : [];

        var items = follows.Select(f => MapUser(f.Following, requestingFollowingIds)).ToList();
        return PagedResponse<UserSummaryResponse>.Create(items, page, pageSize, total);
    }

    public async Task<PagedResponse<UserSummaryResponse>> GetFollowersAsync(
        Guid userId, int page, int pageSize, Guid? requestingUserId)
    {
        var query = db.Follows
            .Where(f => f.FollowingId == userId)
            .Include(f => f.Follower);

        var total = await query.CountAsync();
        var follows = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        HashSet<Guid> requestingFollowingIds = requestingUserId.HasValue
            ? [.. await db.Follows
                .Where(f => f.FollowerId == requestingUserId.Value)
                .Select(f => f.FollowingId)
                .ToListAsync()]
            : [];

        var items = follows.Select(f => MapUser(f.Follower, requestingFollowingIds)).ToList();
        return PagedResponse<UserSummaryResponse>.Create(items, page, pageSize, total);
    }

    public async Task<FollowStatusResponse> GetFollowStatusAsync(Guid requestingUserId, Guid targetUserId)
    {
        _ = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        return await BuildFollowStatusAsync(requestingUserId, targetUserId);
    }

    // -- helpers --

    private async Task<FollowStatusResponse> BuildFollowStatusAsync(Guid requestingUserId, Guid targetUserId)
    {
        var isFollowing = await db.Follows
            .AnyAsync(f => f.FollowerId == requestingUserId && f.FollowingId == targetUserId);
        var isFollowedBy = await db.Follows
            .AnyAsync(f => f.FollowerId == targetUserId && f.FollowingId == requestingUserId);
        var followersCount = await db.Follows.CountAsync(f => f.FollowingId == targetUserId);
        var followingCount = await db.Follows.CountAsync(f => f.FollowerId == targetUserId);

        return new FollowStatusResponse
        {
            IsFollowing = isFollowing,
            IsFollowedBy = isFollowedBy,
            IsMutual = isFollowing && isFollowedBy,
            FollowersCount = followersCount,
            FollowingCount = followingCount
        };
    }

    private UserSummaryResponse MapUser(User user, HashSet<Guid> requestingFollowingIds)
    {
        var followersCount = db.Follows.Count(f => f.FollowingId == user.Id);
        return new UserSummaryResponse
        {
            Id = user.Id,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            Level = user.Level,
            FollowersCount = followersCount,
            IsFollowing = requestingFollowingIds.Contains(user.Id)
        };
    }
}
