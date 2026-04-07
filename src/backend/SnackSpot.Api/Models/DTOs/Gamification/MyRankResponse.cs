namespace SnackSpot.Api.Models.DTOs.Gamification;

public class MyRankResponse
{
    public string Type { get; set; } = string.Empty;
    public int? Rank { get; set; }
    public int Score { get; set; }
}
