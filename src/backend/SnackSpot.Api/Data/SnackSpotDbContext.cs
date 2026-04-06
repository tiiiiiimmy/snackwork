using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Data;

public class SnackSpotDbContext(DbContextOptions<SnackSpotDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<Snack> Snacks { get; set; }
    public DbSet<SnackImage> SnackImages { get; set; }
    public DbSet<SnackTag> SnackTags { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ReviewLike> ReviewLikes { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<UserLevel> UserLevels { get; set; }
    public DbSet<Leaderboard> Leaderboards { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<UserViewHistory> UserViewHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50);
            entity.Property(u => u.Email).HasMaxLength(255);
            entity.Property(u => u.PasswordHash).HasMaxLength(255);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.Property(u => u.Bio).HasMaxLength(200);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(r => r.Token).IsUnique();
            entity.Property(r => r.Token).HasMaxLength(36);
            entity.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(50);
            entity.Property(c => c.Description).HasMaxLength(200);
            entity.Property(c => c.Icon).HasMaxLength(50);
            entity.HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Store
        modelBuilder.Entity<Store>(entity =>
        {
            entity.Property(s => s.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(s => s.Longitude).HasColumnType("decimal(9,6)");
            entity.Property(s => s.Name).HasMaxLength(80);
            entity.Property(s => s.Address).HasMaxLength(120);
            entity.Property(s => s.Description).HasMaxLength(200);
            entity.HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Snack
        modelBuilder.Entity<Snack>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(100);
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.Property(s => s.Price).HasColumnType("decimal(10,2)");
            entity.Property(s => s.AverageRating).HasColumnType("decimal(3,2)");
            entity.HasOne(s => s.Category)
                .WithMany(c => c.Snacks)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.Store)
                .WithMany(st => st.Snacks)
                .HasForeignKey(s => s.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.CreatedBy)
                .WithMany(u => u.Snacks)
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SnackImage
        modelBuilder.Entity<SnackImage>(entity =>
        {
            entity.Property(i => i.ImageUrl).HasMaxLength(500);
            entity.HasOne(i => i.Snack)
                .WithMany(s => s.Images)
                .HasForeignKey(i => i.SnackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SnackTag
        modelBuilder.Entity<SnackTag>(entity =>
        {
            entity.Property(t => t.TagName).HasMaxLength(50);
            entity.HasOne(t => t.Snack)
                .WithMany(s => s.Tags)
                .HasForeignKey(t => t.SnackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Review
        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(r => r.Comment).HasMaxLength(500);
            entity.HasIndex(r => new { r.SnackId, r.UserId }).IsUnique();
            entity.HasOne(r => r.Snack)
                .WithMany(s => s.Reviews)
                .HasForeignKey(r => r.SnackId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ReviewLike
        modelBuilder.Entity<ReviewLike>(entity =>
        {
            entity.HasIndex(l => new { l.ReviewId, l.UserId }).IsUnique();
            entity.HasOne(l => l.Review)
                .WithMany(r => r.Likes)
                .HasForeignKey(l => l.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Follow
        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();
            entity.HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.Property(m => m.Content).HasMaxLength(1000);
            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserLevel
        modelBuilder.Entity<UserLevel>(entity =>
        {
            entity.HasKey(l => l.Level);
            entity.Property(l => l.Title).HasMaxLength(50);
            entity.Property(l => l.Description).HasMaxLength(200);
        });

        // Leaderboard
        modelBuilder.Entity<Leaderboard>(entity =>
        {
            entity.HasIndex(l => new { l.Type, l.UserId }).IsUnique();
            entity.Property(l => l.Type).HasMaxLength(50);
            entity.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Achievement
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.Property(a => a.Name).HasMaxLength(100);
            entity.Property(a => a.Description).HasMaxLength(200);
            entity.Property(a => a.Icon).HasMaxLength(50);
            entity.Property(a => a.ConditionType).HasMaxLength(50);
        });

        // UserAchievement
        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasIndex(ua => new { ua.UserId, ua.AchievementId }).IsUnique();
            entity.HasOne(ua => ua.User)
                .WithMany()
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ua => ua.Achievement)
                .WithMany(a => a.UserAchievements)
                .HasForeignKey(ua => ua.AchievementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserViewHistory
        modelBuilder.Entity<UserViewHistory>(entity =>
        {
            entity.HasKey(v => new { v.UserId, v.SnackId });
            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(v => v.Snack)
                .WithMany(s => s.ViewHistory)
                .HasForeignKey(v => v.SnackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed UserLevels
        modelBuilder.Entity<UserLevel>().HasData(
            new UserLevel { Level = 1, MinExperience = 0, MaxExperience = 99, Title = "Newcomer", Description = "Just getting started on the snack journey.", UnlockedFeatures = null },
            new UserLevel { Level = 2, MinExperience = 100, MaxExperience = 299, Title = "Explorer", Description = "Beginning to explore the snack world.", UnlockedFeatures = "[\"create_category\"]" },
            new UserLevel { Level = 3, MinExperience = 300, MaxExperience = 599, Title = "Enthusiast", Description = "A growing passion for discovering snacks.", UnlockedFeatures = "[\"follow_users\"]" },
            new UserLevel { Level = 4, MinExperience = 600, MaxExperience = 999, Title = "Connoisseur", Description = "Developing a refined snack palate.", UnlockedFeatures = null },
            new UserLevel { Level = 5, MinExperience = 1000, MaxExperience = 1499, Title = "Expert", Description = "A seasoned snack aficionado.", UnlockedFeatures = "[\"send_messages\"]" },
            new UserLevel { Level = 6, MinExperience = 1500, MaxExperience = 2199, Title = "Guru", Description = "Deep knowledge of the snack ecosystem.", UnlockedFeatures = null },
            new UserLevel { Level = 7, MinExperience = 2200, MaxExperience = 2999, Title = "Master", Description = "Mastery over snack discovery and curation.", UnlockedFeatures = "[\"create_store\"]" },
            new UserLevel { Level = 8, MinExperience = 3000, MaxExperience = 3999, Title = "Legend", Description = "A legendary figure in the snack community.", UnlockedFeatures = null },
            new UserLevel { Level = 9, MinExperience = 4000, MaxExperience = 4999, Title = "Mythic", Description = "Mythic status among snack enthusiasts.", UnlockedFeatures = null },
            new UserLevel { Level = 10, MinExperience = 5000, MaxExperience = 2147483647, Title = "Transcendent", Description = "Transcended all limits of snack knowledge.", UnlockedFeatures = "[\"all\"]" }
        );
    }
}
