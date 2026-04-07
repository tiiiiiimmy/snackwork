using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/levels")]
public class LevelsController(IGamificationService gamificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllLevels()
    {
        var result = await gamificationService.GetAllLevelConfigsAsync();
        return Ok(ApiResponse<object>.Ok(result));
    }
}
