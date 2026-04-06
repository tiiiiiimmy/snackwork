namespace SnackSpot.Api.Models.Entities;

public class ReviewLike
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Review Review { get; set; } = null!;
    public User User { get; set; } = null!;
}
