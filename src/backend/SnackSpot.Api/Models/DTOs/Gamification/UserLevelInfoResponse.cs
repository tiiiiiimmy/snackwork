namespace SnackSpot.Api.Models.DTOs.Gamification;

public class UserLevelInfoResponse
{
    public int Level { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ExperiencePoints { get; set; }
    public int MinExperience { get; set; }
    public int MaxExperience { get; set; }
    public int? NextLevelMinExperience { get; set; }
    public int? ExperienceToNextLevel { get; set; }
}
