using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Infrastructure.Storage;
using SnackSpot.Api.Models.DTOs.Images;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/images")]
[Authorize]
public class ImagesController(IR2StorageService r2) : ControllerBase
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private static readonly Regex FileNamePattern = new(@"^[\w\-\.]+\.(jpg|jpeg|png|webp)$", RegexOptions.IgnoreCase);

    [HttpGet("presign")]
    public async Task<IActionResult> GetPresignedUrl(
        [FromQuery] string fileName,
        [FromQuery] string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !FileNamePattern.IsMatch(fileName))
            return BadRequest(ApiResponse.Fail("Invalid file name. Must match pattern: name.jpg|jpeg|png|webp"));

        if (!AllowedContentTypes.Contains(contentType))
            return BadRequest(ApiResponse.Fail("Invalid content type. Allowed: image/jpeg, image/png, image/webp"));

        var (uploadUrl, imageUrl) = await r2.GeneratePresignedPutUrlAsync(
            fileName, contentType, TimeSpan.FromMinutes(5));

        return Ok(ApiResponse<PresignResponse>.Ok(new PresignResponse
        {
            UploadUrl = uploadUrl,
            ImageUrl = imageUrl
        }));
    }
}
