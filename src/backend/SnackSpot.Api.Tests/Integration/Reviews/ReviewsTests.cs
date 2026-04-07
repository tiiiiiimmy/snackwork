using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Tests.Integration.Reviews;

public class ReviewsTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public ReviewsTests()
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

        var category = new Category { Name = "ReviewTestCat_" + Guid.NewGuid() };
        var store = new Store { Name = "ReviewTestStore_" + Guid.NewGuid(), Latitude = 0, Longitude = 0, CreatedByUserId = Guid.NewGuid() };
        db.Categories.Add(category);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        var snack = new Snack
        {
            Name = "ReviewTestSnack_" + Guid.NewGuid(),
            CategoryId = category.Id,
            StoreId = store.Id,
            CreatedByUserId = Guid.NewGuid()
        };
        db.Snacks.Add(snack);
        await db.SaveChangesAsync();
        return snack.Id;
    }

    private async Task<Guid> CreateReviewAsync(Guid snackId, string token, int rating = 4, string? comment = "Great snack!")
    {
        SetBearer(token);
        var resp = await _client.PostAsync($"/api/v1/snacks/{snackId}/reviews",
            TestHelpers.JsonContent(new { Rating = rating, Comment = comment }));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<ReviewData>>(body, TestHelpers.JsonOptions)!;
        return result.Data!.Id;
    }

    // -- tests --

    [Fact]
    public async Task GetReviews_EmptySnack_ReturnsEmptyPage()
    {
        var snackId = await SeedSnackAsync();

        var resp = await _client.GetAsync($"/api/v1/snacks/{snackId}/reviews");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<ReviewData>>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
    }

    [Fact]
    public async Task CreateReview_ValidRequest_Returns201()
    {
        var snackId = await SeedSnackAsync();
        var (token, _) = await RegisterAndGetTokenAsync("reviewer1", "reviewer1@test.com");
        SetBearer(token);

        var resp = await _client.PostAsync($"/api/v1/snacks/{snackId}/reviews",
            TestHelpers.JsonContent(new { Rating = 5, Comment = "Absolutely delicious!" }));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<ReviewData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Rating.Should().Be(5);
        result.Data.Comment.Should().Be("Absolutely delicious!");
        result.Data.SnackId.Should().Be(snackId);
    }

    [Fact]
    public async Task CreateReview_DuplicateReview_Returns400()
    {
        var snackId = await SeedSnackAsync();
        var (token, _) = await RegisterAndGetTokenAsync("reviewer2", "reviewer2@test.com");

        await CreateReviewAsync(snackId, token, 3);

        SetBearer(token);
        var resp = await _client.PostAsync($"/api/v1/snacks/{snackId}/reviews",
            TestHelpers.JsonContent(new { Rating = 4, Comment = "Trying again" }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<ReviewData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateReview_InvalidRating_Returns400()
    {
        var snackId = await SeedSnackAsync();
        var (token, _) = await RegisterAndGetTokenAsync("reviewer3", "reviewer3@test.com");
        SetBearer(token);

        var resp = await _client.PostAsync($"/api/v1/snacks/{snackId}/reviews",
            TestHelpers.JsonContent(new { Rating = 6 }));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReview_ById_Returns200()
    {
        var snackId = await SeedSnackAsync();
        var (token, _) = await RegisterAndGetTokenAsync("reviewer4", "reviewer4@test.com");
        var reviewId = await CreateReviewAsync(snackId, token, 3, "Decent");

        var resp = await _client.GetAsync($"/api/v1/reviews/{reviewId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<ReviewData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Id.Should().Be(reviewId);
        result.Data.Comment.Should().Be("Decent");
    }

    [Fact]
    public async Task UpdateReview_Owner_Returns200()
    {
        var snackId = await SeedSnackAsync();
        var (token, _) = await RegisterAndGetTokenAsync("reviewer5", "reviewer5@test.com");
        var reviewId = await CreateReviewAsync(snackId, token, 3, "OK");

        SetBearer(token);
        var resp = await _client.PutAsync($"/api/v1/reviews/{reviewId}",
            TestHelpers.JsonContent(new { Rating = 5, Comment = "Changed my mind, amazing!" }));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<ReviewData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Rating.Should().Be(5);
        result.Data.Comment.Should().Be("Changed my mind, amazing!");
    }

    [Fact]
    public async Task UpdateReview_NotOwner_Returns401()
    {
        var snackId = await SeedSnackAsync();
        var (tokenA, _) = await RegisterAndGetTokenAsync("reviewerA", "reviewerA@test.com");
        var reviewId = await CreateReviewAsync(snackId, tokenA, 4, "Good");

        var (tokenB, _) = await RegisterAndGetTokenAsync("reviewerB", "reviewerB@test.com");
        SetBearer(tokenB);

        var resp = await _client.PutAsync($"/api/v1/reviews/{reviewId}",
            TestHelpers.JsonContent(new { Rating = 1 }));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteReview_Owner_Returns200_ThenGet404()
    {
        var snackId = await SeedSnackAsync();
        var (token, _) = await RegisterAndGetTokenAsync("reviewer6", "reviewer6@test.com");
        var reviewId = await CreateReviewAsync(snackId, token, 2, "Not great");

        SetBearer(token);
        var deleteResp = await _client.DeleteAsync($"/api/v1/reviews/{reviewId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await _client.GetAsync($"/api/v1/reviews/{reviewId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleLike_LikeThenUnlike_TogglesCorrectly()
    {
        var snackId = await SeedSnackAsync();
        var (tokenOwner, _) = await RegisterAndGetTokenAsync("reviewer7", "reviewer7@test.com");
        var reviewId = await CreateReviewAsync(snackId, tokenOwner, 4);

        var (tokenLiker, _) = await RegisterAndGetTokenAsync("liker1", "liker1@test.com");
        SetBearer(tokenLiker);

        // First like
        var likeResp = await _client.PostAsync($"/api/v1/reviews/{reviewId}/like", null);
        likeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var likeBody = await likeResp.Content.ReadAsStringAsync();
        var likeResult = JsonSerializer.Deserialize<ApiResponseWrapper<LikeData>>(likeBody, TestHelpers.JsonOptions);
        likeResult!.Data!.IsLiked.Should().BeTrue();
        likeResult.Data.LikeCount.Should().Be(1);

        // Second call — unlike
        var unlikeResp = await _client.PostAsync($"/api/v1/reviews/{reviewId}/like", null);
        var unlikeBody = await unlikeResp.Content.ReadAsStringAsync();
        var unlikeResult = JsonSerializer.Deserialize<ApiResponseWrapper<LikeData>>(unlikeBody, TestHelpers.JsonOptions);
        unlikeResult!.Data!.IsLiked.Should().BeFalse();
        unlikeResult.Data.LikeCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateReview_UpdatesSnackStats()
    {
        var snackId = await SeedSnackAsync();
        var (token, _) = await RegisterAndGetTokenAsync("reviewer8", "reviewer8@test.com");

        await CreateReviewAsync(snackId, token, 4, "Good stuff");

        var resp = await _client.GetAsync($"/api/v1/snacks/{snackId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<SnackData>>(body, TestHelpers.JsonOptions);
        result!.Data!.TotalReviews.Should().Be(1);
        result.Data.AverageRating.Should().Be(4m);
    }

    // local response shapes
    private record AuthData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username);
    private record ReviewData(Guid Id, Guid SnackId, Guid UserId, string Username, int Rating, string? Comment, int LikeCount);
    private record LikeData(bool IsLiked, int LikeCount);
    private record SnackData(Guid Id, string Name, decimal AverageRating, int TotalReviews, int TotalRatings);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
