namespace SnackSpot.Api.Models.DTOs.Users;

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public int SnacksCount { get; set; }
    public int ReviewsCount { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
