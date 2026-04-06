namespace SnackSpot.Api.Models.Entities;

public class SnackTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SnackId { get; set; }
    public string TagName { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Snack Snack { get; set; } = null!;
}
