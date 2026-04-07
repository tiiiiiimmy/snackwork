namespace SnackSpot.Api.Models.DTOs.Gamification;

public class LeaderboardEntryResponse
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Level { get; set; }
    public int Score { get; set; }
}
