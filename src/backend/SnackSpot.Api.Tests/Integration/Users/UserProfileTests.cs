using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Tests.Integration.Users;

public class UserProfileTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public UserProfileTests()
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

    private async Task<Guid> SeedSnackAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnackSpotDbContext>();
        var category = new Category { Name = "UserProfCat_" + Guid.NewGuid() };
        var store = new Store { Name = "UserProfStore_" + Guid.NewGuid(), Latitude = 0, Longitude = 0, CreatedByUserId = userId };
        db.Categories.Add(category);
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        var snack = new Snack { Name = "UserProfSnack_" + Guid.NewGuid(), CategoryId = category.Id, StoreId = store.Id, CreatedByUserId = userId };
        db.Snacks.Add(snack);
        await db.SaveChangesAsync();
        return snack.Id;
    }

    // -- tests --

    [Fact]
    public async Task GetUserProfile_ValidUser_Returns200()
    {
        var (_, userId) = await RegisterAndGetTokenAsync("profuser1", "profuser1@test.com");

        var resp = await _client.GetAsync($"/api/v1/users/{userId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<UserProfileData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Id.Should().Be(userId);
        result.Data.Username.Should().Be("profuser1");
        result.Data.FollowersCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserProfile_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUserProfile_Self_Returns200()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("profuser2", "profuser2@test.com");
        SetBearer(token);

        var resp = await _client.PutAsync($"/api/v1/users/{userId}",
            TestHelpers.JsonContent(new { Bio = "I love snacks!" }));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<UserProfileData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Bio.Should().Be("I love snacks!");
    }

    [Fact]
    public async Task UpdateUserProfile_NotOwner_Returns401()
    {
        var (_, targetId) = await RegisterAndGetTokenAsync("profuser3", "profuser3@test.com");
        var (tokenOther, _) = await RegisterAndGetTokenAsync("profuser4", "profuser4@test.com");
        SetBearer(tokenOther);

        var resp = await _client.PutAsync($"/api/v1/users/{targetId}",
            TestHelpers.JsonContent(new { Bio = "Hacked!" }));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserSnacks_Returns200WithList()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("profuser5", "profuser5@test.com");
        await SeedSnackAsync(userId);

        var resp = await _client.GetAsync($"/api/v1/users/{userId}/snacks");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<SnackData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetUserReviews_Returns200WithList()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("profuser6", "profuser6@test.com");
        var snackId = await SeedSnackAsync(Guid.NewGuid());
        SetBearer(token);
        await _client.PostAsync($"/api/v1/snacks/{snackId}/reviews",
            TestHelpers.JsonContent(new { Rating = 4, Comment = "Nice" }));

        var resp = await _client.GetAsync($"/api/v1/users/{userId}/reviews");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<ReviewData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    // local shapes
    private record AuthData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username);
    private record UserProfileData(Guid Id, string Username, string? Bio, int Level, int FollowersCount, int SnacksCount);
    private record SnackData(Guid Id, string Name);
    private record ReviewData(Guid Id, int Rating);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
