namespace SnackSpot.Api.Models.DTOs.Snacks;

public class CreateSnackRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Guid StoreId { get; set; }
    public decimal? Price { get; set; }
    public string[]? ImageUrls { get; set; }
    public string[]? Tags { get; set; }
}
