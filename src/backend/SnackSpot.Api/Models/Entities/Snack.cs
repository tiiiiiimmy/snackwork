namespace SnackSpot.Api.Models.Entities;

public class Snack
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Guid StoreId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public decimal? Price { get; set; }
    public decimal AverageRating { get; set; } = 0;
    public int TotalRatings { get; set; } = 0;
    public int TotalReviews { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    public Category Category { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<SnackImage> Images { get; set; } = [];
    public ICollection<SnackTag> Tags { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<UserViewHistory> ViewHistory { get; set; } = [];
}
