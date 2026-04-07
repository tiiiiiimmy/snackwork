using SnackSpot.Api.Models.DTOs.Snacks;

namespace SnackSpot.Api.Models.DTOs.Feed;

public class RecommendationItem
{
    public SnackResponse Snack { get; set; } = null!;
    public string RecommendationReason { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Source { get; set; } = string.Empty;
}
