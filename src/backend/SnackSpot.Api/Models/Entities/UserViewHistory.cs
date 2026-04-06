namespace SnackSpot.Api.Models.Entities;

public class UserViewHistory
{
    public Guid UserId { get; set; }
    public Guid SnackId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Snack Snack { get; set; } = null!;
}
