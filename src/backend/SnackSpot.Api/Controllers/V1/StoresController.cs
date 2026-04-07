using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Stores;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/stores")]
public class StoresController(IStoreService storeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStores(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        var result = await storeService.GetStoresAsync(search, page, pageSize);
        return Ok(ApiResponse<PagedResponse<StoreResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStore(Guid id)
    {
        var result = await storeService.GetStoreAsync(id);
        return Ok(ApiResponse<StoreResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await storeService.CreateStoreAsync(request, userId);
        return StatusCode(201, ApiResponse<StoreResponse>.Ok(result, "Store created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateStore(Guid id, [FromBody] UpdateStoreRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await storeService.UpdateStoreAsync(id, request, userId);
        return Ok(ApiResponse<StoreResponse>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteStore(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await storeService.DeleteStoreAsync(id, userId);
        return Ok(ApiResponse.Ok("Store deleted."));
    }
}
