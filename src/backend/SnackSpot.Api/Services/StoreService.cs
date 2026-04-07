using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Common;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Stores;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Services;

public class StoreService(SnackSpotDbContext db) : IStoreService
{
    public async Task<PagedResponse<StoreResponse>> GetStoresAsync(string? search, int page, int pageSize)
    {
        var query = db.Stores.Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.Name.Contains(search) ||
                (s.Address != null && s.Address.Contains(search)));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => ToResponse(s))
            .ToListAsync();

        return PagedResponse<StoreResponse>.Create(items, page, pageSize, total);
    }

    public async Task<StoreResponse> GetStoreAsync(Guid id)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            ?? throw new KeyNotFoundException($"Store {id} not found.");
        return ToResponse(store);
    }

    public async Task<StoreResponse> CreateStoreAsync(CreateStoreRequest request, Guid userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Level < 7)
            throw new InvalidOperationException("Insufficient level to create stores (requires Level 7).");

        var store = new Store
        {
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Description = request.Description,
            CreatedByUserId = userId
        };

        db.Stores.Add(store);
        await db.SaveChangesAsync();

        return ToResponse(store);
    }

    public async Task<StoreResponse> UpdateStoreAsync(Guid id, UpdateStoreRequest request, Guid userId)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            ?? throw new KeyNotFoundException($"Store {id} not found.");

        if (store.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Not the owner of this store.");

        if (request.Name is not null) store.Name = request.Name;
        if (request.Address is not null) store.Address = request.Address;
        if (request.Latitude.HasValue) store.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) store.Longitude = request.Longitude.Value;
        if (request.Description is not null) store.Description = request.Description;

        await db.SaveChangesAsync();
        return ToResponse(store);
    }

    public async Task DeleteStoreAsync(Guid id, Guid userId)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            ?? throw new KeyNotFoundException($"Store {id} not found.");

        if (store.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Not the owner of this store.");

        store.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    private static StoreResponse ToResponse(Store s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Address = s.Address,
        Latitude = s.Latitude,
        Longitude = s.Longitude,
        Description = s.Description,
        CreatedByUserId = s.CreatedByUserId,
        CreatedAt = s.CreatedAt
    };
}
