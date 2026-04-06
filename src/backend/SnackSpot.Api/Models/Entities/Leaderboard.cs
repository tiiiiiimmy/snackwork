namespace SnackSpot.Api.Models.Entities;

public class Leaderboard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = null!;
    public Guid UserId { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
