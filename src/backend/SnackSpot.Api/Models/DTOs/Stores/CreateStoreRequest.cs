namespace SnackSpot.Api.Models.DTOs.Stores;

public class CreateStoreRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Description { get; set; }
}
