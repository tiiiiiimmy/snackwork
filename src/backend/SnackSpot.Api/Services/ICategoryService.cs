using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Categories;

namespace SnackSpot.Api.Services;

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> GetCategoriesAsync(int page, int pageSize);
    Task<CategoryResponse> GetCategoryAsync(Guid id);
    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, Guid userId);
    Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, Guid userId);
    Task DeleteCategoryAsync(Guid id, Guid userId);
}
