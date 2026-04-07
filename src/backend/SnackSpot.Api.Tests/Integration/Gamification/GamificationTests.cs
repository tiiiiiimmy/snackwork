using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Tests.Integration.Gamification;

public class GamificationTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public GamificationTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // -- helpers --

    private async Task<(string token, Guid userId)> RegisterAndGetTokenAsync(string username, string email)
    {
        var resp = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(new
        {
            Username = username, Email = email, Password = "Password123!"
        }));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<AuthData>>(body, TestHelpers.JsonOptions)!;
        return (result.Data!.AccessToken, result.Data.User.Id);
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> SeedSnackAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnackSpotDbContext>();
        var category = new Category { Name = "GamCat_" + Guid.NewGuid() };
        var store = new Store { Name = "GamStore_" + Guid.NewGuid(), Latitude = 0, Longitude = 0, CreatedByUserId = Guid.NewGuid() };
        db.Categories.Add(category);
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        var snack = new Snack { Name = "GamSnack_" + Guid.NewGuid(), CategoryId = category.Id, StoreId = store.Id, CreatedByUserId = Guid.NewGuid() };
        db.Snacks.Add(snack);
        await db.SaveChangesAsync();
        return snack.Id;
    }

    private async Task<int> GetUserXpAsync(Guid userId)
    {
        var resp = await _client.GetAsync($"/api/v1/users/{userId}/level");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<LevelInfoData>>(body, TestHelpers.JsonOptions)!;
        return result.Data!.ExperiencePoints;
    }

    // -- tests --

    [Fact]
    public async Task CreateSnack_AwardsXp_UserXpIncreased()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("gamuser1", "gamuser1@test.com");
        SetBearer(token);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnackSpotDbContext>();
        var category = new Category { Name = "GamSnackCat_" + Guid.NewGuid() };
        var store = new Store { Name = "GamSnackStore_" + Guid.NewGuid(), Latitude = 0, Longitude = 0, CreatedByUserId = userId };
        db.Categories.Add(category);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var resp = await _client.PostAsync("/api/v1/snacks", TestHelpers.JsonContent(new
        {
            Name = "XP Test Snack", CategoryId = category.Id, StoreId = store.Id
        }));
        resp.EnsureSuccessStatusCode();

        var xp = await GetUserXpAsync(userId);
        xp.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public async Task CreateReview_AwardsXp_UserXpIncreased()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("gamuser2", "gamuser2@test.com");
        var snackId = await SeedSnackAsync();
        SetBearer(token);

        var resp = await _client.PostAsync($"/api/v1/snacks/{snackId}/reviews",
            TestHelpers.JsonContent(new { Rating = 4, Comment = "Good" }));
        resp.EnsureSuccessStatusCode();

        var xp = await GetUserXpAsync(userId);
        xp.Should().BeGreaterThanOrEqualTo(20);
    }

    [Fact]
    public async Task ToggleLike_AwardsXpToReviewOwner()
    {
        var (tokenOwner, ownerId) = await RegisterAndGetTokenAsync("gamuser3", "gamuser3@test.com");
        var (tokenLiker, _) = await RegisterAndGetTokenAsync("gamuser4", "gamuser4@test.com");
        var snackId = await SeedSnackAsync();

        SetBearer(tokenOwner);
        var reviewResp = await _client.PostAsync($"/api/v1/snacks/{snackId}/reviews",
            TestHelpers.JsonContent(new { Rating = 5, Comment = "Amazing" }));
        reviewResp.EnsureSuccessStatusCode();
        var reviewBody = await reviewResp.Content.ReadAsStringAsync();
        var reviewResult = JsonSerializer.Deserialize<ApiResponseWrapper<ReviewData>>(reviewBody, TestHelpers.JsonOptions)!;
        var reviewId = reviewResult.Data!.Id;

        var xpBefore = await GetUserXpAsync(ownerId);

        SetBearer(tokenLiker);
        await _client.PostAsync($"/api/v1/reviews/{reviewId}/like", null);

        var xpAfter = await GetUserXpAsync(ownerId);
        xpAfter.Should().Be(xpBefore + 5);
    }

    [Fact]
    public async Task Follow_AwardsXpToFollowedUser()
    {
        var (tokenFollower, _) = await RegisterAndGetTokenAsync("gamuser5", "gamuser5@test.com");
        var (_, followedId) = await RegisterAndGetTokenAsync("gamuser6", "gamuser6@test.com");

        var xpBefore = await GetUserXpAsync(followedId);

        SetBearer(tokenFollower);
        await _client.PostAsync($"/api/v1/users/{followedId}/follow", null);

        var xpAfter = await GetUserXpAsync(followedId);
        xpAfter.Should().Be(xpBefore + 10);
    }

    [Fact]
    public async Task GetUserLevel_Returns200WithLevelInfo()
    {
        var (_, userId) = await RegisterAndGetTokenAsync("gamuser7", "gamuser7@test.com");

        var resp = await _client.GetAsync($"/api/v1/users/{userId}/level");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<LevelInfoData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Level.Should().Be(1);
        result.Data.Title.Should().Be("Newcomer");
    }

    [Fact]
    public async Task GetAllLevels_Returns200With10Levels()
    {
        var resp = await _client.GetAsync("/api/v1/levels");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<IReadOnlyList<LevelData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Count.Should().Be(10);
    }

    // local shapes
    private record AuthData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username);
    private record LevelInfoData(int Level, string Title, int ExperiencePoints, int MinExperience);
    private record LevelData(int Level, string Title, int MinExperience);
    private record ReviewData(Guid Id, Guid UserId, int Rating);
}
