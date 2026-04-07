namespace SnackSpot.Api.Models.DTOs.Images;

public class PresignResponse
{
    public string UploadUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
