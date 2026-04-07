using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Snacks;

namespace SnackSpot.Api.Services;

public interface ISnackService
{
    Task<PagedResponse<SnackResponse>> GetSnacksAsync(Guid? categoryId, Guid? storeId, string? search, int page, int pageSize);
    Task<SnackResponse> GetSnackAsync(Guid id);
    Task<SnackResponse> CreateSnackAsync(CreateSnackRequest request, Guid userId);
    Task<SnackResponse> UpdateSnackAsync(Guid id, UpdateSnackRequest request, Guid userId);
    Task DeleteSnackAsync(Guid id, Guid userId);
    Task<PagedResponse<SnackResponse>> SearchSnacksAsync(
        string? q, Guid? categoryId, decimal? minPrice, decimal? maxPrice,
        decimal? minRating, string? sort, int page, int limit);
}
