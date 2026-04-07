using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Common;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Snacks;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Services;

public class SnackService(SnackSpotDbContext db, IGamificationService gamification) : ISnackService
{
    public async Task<PagedResponse<SnackResponse>> GetSnacksAsync(
        Guid? categoryId, Guid? storeId, string? search, int page, int pageSize)
    {
        var query = db.Snacks
            .Include(s => s.Category)
            .Include(s => s.Store)
            .Include(s => s.Images)
            .Include(s => s.Tags)
            .Where(s => !s.IsDeleted);

        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        if (storeId.HasValue)
            query = query.Where(s => s.StoreId == storeId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResponse<SnackResponse>.Create(
            items.Select(ToResponse).ToList(), page, pageSize, total);
    }

    public async Task<SnackResponse> GetSnackAsync(Guid id)
    {
        var snack = await db.Snacks
            .Include(s => s.Category)
            .Include(s => s.Store)
            .Include(s => s.Images)
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            ?? throw new KeyNotFoundException($"Snack {id} not found.");

        return ToResponse(snack);
    }

    public async Task<SnackResponse> CreateSnackAsync(CreateSnackRequest request, Guid userId)
    {
        var categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted);
        if (!categoryExists)
            throw new KeyNotFoundException($"Category {request.CategoryId} not found.");

        var storeExists = await db.Stores.AnyAsync(s => s.Id == request.StoreId && !s.IsDeleted);
        if (!storeExists)
            throw new KeyNotFoundException($"Store {request.StoreId} not found.");

        var snack = new Snack
        {
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            StoreId = request.StoreId,
            CreatedByUserId = userId,
            Price = request.Price
        };

        if (request.ImageUrls is { Length: > 0 })
        {
            for (var i = 0; i < request.ImageUrls.Length; i++)
                snack.Images.Add(new SnackImage { ImageUrl = request.ImageUrls[i], DisplayOrder = i });
        }

        if (request.Tags is { Length: > 0 })
        {
            foreach (var tag in request.Tags)
                snack.Tags.Add(new SnackTag { TagName = tag });
        }

        db.Snacks.Add(snack);
        await db.SaveChangesAsync();
        await gamification.AwardXpAsync(userId, 50);

        // Reload with navigation props for accurate response
        return await GetSnackAsync(snack.Id);
    }

    public async Task<SnackResponse> UpdateSnackAsync(Guid id, UpdateSnackRequest request, Guid userId)
    {
        // Load without images/tags to avoid navigation-collection tracking conflicts
        var snack = await db.Snacks
            .Include(s => s.Category)
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            ?? throw new KeyNotFoundException($"Snack {id} not found.");

        if (snack.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Not the owner of this snack.");

        if (request.Name is not null) snack.Name = request.Name;
        if (request.Description is not null) snack.Description = request.Description;
        if (request.Price.HasValue) snack.Price = request.Price.Value;

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value && !c.IsDeleted);
            if (!categoryExists)
                throw new KeyNotFoundException($"Category {request.CategoryId.Value} not found.");
            snack.CategoryId = request.CategoryId.Value;
        }

        if (request.StoreId.HasValue)
        {
            var storeExists = await db.Stores.AnyAsync(s => s.Id == request.StoreId.Value && !s.IsDeleted);
            if (!storeExists)
                throw new KeyNotFoundException($"Store {request.StoreId.Value} not found.");
            snack.StoreId = request.StoreId.Value;
        }

        if (request.ImageUrls is not null)
        {
            // Query DbSet directly (avoids navigation-collection tracking issues with InMemory)
            var oldImages = await db.SnackImages.Where(i => i.SnackId == id).ToListAsync();
            db.SnackImages.RemoveRange(oldImages);
            for (var i = 0; i < request.ImageUrls.Length; i++)
                db.SnackImages.Add(new SnackImage { SnackId = id, ImageUrl = request.ImageUrls[i], DisplayOrder = i });
        }

        if (request.Tags is not null)
        {
            var oldTags = await db.SnackTags.Where(t => t.SnackId == id).ToListAsync();
            db.SnackTags.RemoveRange(oldTags);
            foreach (var tag in request.Tags)
                db.SnackTags.Add(new SnackTag { SnackId = id, TagName = tag });
        }

        snack.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await GetSnackAsync(id);
    }

    public async Task DeleteSnackAsync(Guid id, Guid userId)
    {
        var snack = await db.Snacks.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            ?? throw new KeyNotFoundException($"Snack {id} not found.");

        if (snack.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Not the owner of this snack.");

        snack.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    public async Task<PagedResponse<SnackResponse>> SearchSnacksAsync(
        string? q, Guid? categoryId, decimal? minPrice, decimal? maxPrice,
        decimal? minRating, string? sort, int page, int limit)
    {
        var query = db.Snacks
            .Include(s => s.Category)
            .Include(s => s.Store)
            .Include(s => s.Images)
            .Include(s => s.Tags)
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s =>
                s.Name.Contains(q) ||
                s.Store.Name.Contains(q) ||
                s.Tags.Any(t => t.TagName.Contains(q)));

        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(s => s.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(s => s.Price <= maxPrice.Value);

        if (minRating.HasValue)
            query = query.Where(s => s.AverageRating >= minRating.Value);

        IQueryable<Snack> sorted = sort switch
        {
            "rating"  => query.OrderByDescending(s => s.AverageRating),
            "reviews" => query.OrderByDescending(s => s.TotalReviews),
            _         => query.OrderByDescending(s => s.CreatedAt)
        };

        var total = await sorted.CountAsync();
        var items = await sorted
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return PagedResponse<SnackResponse>.Create(
            items.Select(ToResponse).ToList(), page, limit, total);
    }

    private static SnackResponse ToResponse(Snack s) => new()
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
    };
}
