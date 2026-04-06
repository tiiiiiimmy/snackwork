namespace SnackSpot.Api.Models.Entities;

public class SnackImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SnackId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Snack Snack { get; set; } = null!;
}
