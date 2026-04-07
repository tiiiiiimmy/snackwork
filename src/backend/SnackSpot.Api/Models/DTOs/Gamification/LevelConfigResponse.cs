namespace SnackSpot.Api.Models.DTOs.Gamification;

public class LevelConfigResponse
{
    public int Level { get; set; }
    public int MinExperience { get; set; }
    public int MaxExperience { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UnlockedFeatures { get; set; }
}
