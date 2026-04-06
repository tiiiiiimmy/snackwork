namespace SnackSpot.Api.Models.Entities;

public class Achievement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string ConditionType { get; set; } = null!;
    public int ConditionValue { get; set; }
    public int RewardExperience { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserAchievement> UserAchievements { get; set; } = [];
}
