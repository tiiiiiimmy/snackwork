namespace SnackSpot.Api.Models.Entities;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SnackId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int LikeCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    public Snack Snack { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<ReviewLike> Likes { get; set; } = [];
}
