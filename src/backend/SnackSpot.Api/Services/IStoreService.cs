using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Stores;

namespace SnackSpot.Api.Services;

public interface IStoreService
{
    Task<PagedResponse<StoreResponse>> GetStoresAsync(string? search, int page, int pageSize);
    Task<StoreResponse> GetStoreAsync(Guid id);
    Task<StoreResponse> CreateStoreAsync(CreateStoreRequest request, Guid userId);
    Task<StoreResponse> UpdateStoreAsync(Guid id, UpdateStoreRequest request, Guid userId);
    Task DeleteStoreAsync(Guid id, Guid userId);
}
