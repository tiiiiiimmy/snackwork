namespace SnackSpot.Api.Models.DTOs.Follow;

public class UserSummaryResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Level { get; set; }
    public int FollowersCount { get; set; }
    public bool IsFollowing { get; set; }
}
