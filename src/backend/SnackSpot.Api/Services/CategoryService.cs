using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Common;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Categories;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Services;

public class CategoryService(SnackSpotDbContext db) : ICategoryService
{
    public async Task<PagedResponse<CategoryResponse>> GetCategoriesAsync(int page, int pageSize)
    {
        var query = db.Categories.Where(c => !c.IsDeleted);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToResponse(c))
            .ToListAsync();

        return PagedResponse<CategoryResponse>.Create(items, page, pageSize, total);
    }

    public async Task<CategoryResponse> GetCategoryAsync(Guid id)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"Category {id} not found.");
        return ToResponse(category);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, Guid userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Level < 2)
            throw new InvalidOperationException("Insufficient level to create categories (requires Level 2).");

        var nameExists = await db.Categories.AnyAsync(c => c.Name == request.Name && !c.IsDeleted);
        if (nameExists)
            throw new InvalidOperationException($"A category named '{request.Name}' already exists.");

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            CreatedByUserId = userId
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, Guid userId)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        if (category.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Not the owner of this category.");

        if (request.Name is not null)
        {
            var nameExists = await db.Categories.AnyAsync(c => c.Name == request.Name && c.Id != id && !c.IsDeleted);
            if (nameExists)
                throw new InvalidOperationException($"A category named '{request.Name}' already exists.");
            category.Name = request.Name;
        }

        if (request.Description is not null) category.Description = request.Description;
        if (request.Icon is not null) category.Icon = request.Icon;

        await db.SaveChangesAsync();
        return ToResponse(category);
    }

    public async Task DeleteCategoryAsync(Guid id, Guid userId)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        if (category.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Not the owner of this category.");

        category.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        Icon = c.Icon,
        CreatedByUserId = c.CreatedByUserId,
        CreatedAt = c.CreatedAt
    };
}
