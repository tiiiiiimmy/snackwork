namespace SnackSpot.Api.Models.DTOs.Gamification;

public class LeaderboardResponse
{
    public string Type { get; set; } = string.Empty;
    public IReadOnlyList<LeaderboardEntryResponse> Entries { get; set; } = [];
    public DateTime ComputedAt { get; set; }
}
