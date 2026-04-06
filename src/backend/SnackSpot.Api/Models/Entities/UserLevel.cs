namespace SnackSpot.Api.Models.Entities;

public class UserLevel
{
    public int Level { get; set; }
    public int MinExperience { get; set; }
    public int MaxExperience { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? UnlockedFeatures { get; set; }
}
