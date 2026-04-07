using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Tests.Integration.Feed;

public class FeedTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public FeedTests()
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

    private async Task<Guid> SeedSnackAsync(string categoryName, string storeName, string snackName,
        decimal avgRating = 4.0m, int totalReviews = 5)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnackSpotDbContext>();

        var category = new Category { Name = categoryName };
        var store = new Store { Name = storeName, Latitude = -36.86m, Longitude = 174.76m, CreatedByUserId = Guid.NewGuid() };
        db.Categories.Add(category);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var snack = new Snack
        {
            Name = snackName,
            CategoryId = category.Id,
            StoreId = store.Id,
            CreatedByUserId = Guid.NewGuid(),
            AverageRating = avgRating,
            TotalRatings = totalReviews,
            TotalReviews = totalReviews
        };
        db.Snacks.Add(snack);
        await db.SaveChangesAsync();
        return snack.Id;
    }

    // -- tests --

    [Fact]
    public async Task GetRecommendations_NewUser_Returns200WithSnacks()
    {
        await SeedSnackAsync("FeedCat1_" + Guid.NewGuid(), "FeedStore1_" + Guid.NewGuid(), "FeedSnack1_" + Guid.NewGuid());
        await SeedSnackAsync("FeedCat2_" + Guid.NewGuid(), "FeedStore2_" + Guid.NewGuid(), "FeedSnack2_" + Guid.NewGuid());

        var (token, _) = await RegisterAndGetTokenAsync("feeduser1", "feeduser1@test.com");
        SetBearer(token);

        var resp = await _client.GetAsync("/api/v1/feed/recommendations?page=1&limit=20");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<RecommendationData>>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Items.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetRecommendations_ExcludesReviewedSnacks()
    {
        var snackId = await SeedSnackAsync(
            "FeedCat3_" + Guid.NewGuid(), "FeedStore3_" + Guid.NewGuid(), "FeedSnack3_" + Guid.NewGuid());

        var (token, _) = await RegisterAndGetTokenAsync("feeduser2", "feeduser2@test.com");
        SetBearer(token);

        // Review the snack
        await _client.PostAsync($"/api/v1/snacks/{snackId}/reviews",
            TestHelpers.JsonContent(new { Rating = 4, Comment = "Good" }));

        var resp = await _client.GetAsync("/api/v1/feed/recommendations?page=1&limit=20");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<RecommendationData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().NotContain(i => i.Snack.Id == snackId);
    }

    [Fact]
    public async Task GetRecommendations_SecondCall_ReturnsCachedResult()
    {
        await SeedSnackAsync("FeedCat4_" + Guid.NewGuid(), "FeedStore4_" + Guid.NewGuid(), "FeedSnack4_" + Guid.NewGuid());

        var (token, _) = await RegisterAndGetTokenAsync("feeduser3", "feeduser3@test.com");
        SetBearer(token);

        var resp1 = await _client.GetAsync("/api/v1/feed/recommendations?page=1&limit=20");
        var resp2 = await _client.GetAsync("/api/v1/feed/recommendations?page=1&limit=20");

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadAsStringAsync();
        var body2 = await resp2.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<RecommendationData>>>(body1, TestHelpers.JsonOptions);
        var result2 = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<RecommendationData>>>(body2, TestHelpers.JsonOptions);

        result1!.Data!.Total.Should().Be(result2!.Data!.Total);
    }

    // local response shapes
    private record AuthData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username);
    private record RecommendationData(SnackData Snack, string RecommendationReason, decimal Score, string Source);
    private record SnackData(Guid Id, string Name);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
