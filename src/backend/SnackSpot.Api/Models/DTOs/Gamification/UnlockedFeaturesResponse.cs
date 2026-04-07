namespace SnackSpot.Api.Models.DTOs.Gamification;

public class UnlockedFeaturesResponse
{
    public int Level { get; set; }
    public IReadOnlyList<string> Features { get; set; } = [];
}
