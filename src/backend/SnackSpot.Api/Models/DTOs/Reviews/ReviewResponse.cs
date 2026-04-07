namespace SnackSpot.Api.Models.DTOs.Reviews;

public class ReviewResponse
{
    public Guid Id { get; set; }
    public Guid SnackId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LikeResponse
{
    public bool IsLiked { get; set; }
    public int LikeCount { get; set; }
}
