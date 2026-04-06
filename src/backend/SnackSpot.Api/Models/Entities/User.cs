namespace SnackSpot.Api.Models.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public int Level { get; set; } = 1;
    public int ExperiencePoints { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    public ICollection<Snack> Snacks { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Follow> Following { get; set; } = [];
    public ICollection<Follow> Followers { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
