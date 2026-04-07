using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace SnackSpot.Api.Tests.Integration.Follows;

public class FollowTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public FollowTests()
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

    // -- tests --

    [Fact]
    public async Task FollowUser_ValidUser_Returns200()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("follower1", "follower1@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("target1", "target1@test.com");

        SetBearer(tokenA);
        var resp = await _client.PostAsync($"/api/v1/users/{userBId}/follow", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<FollowStatusData>>(body, TestHelpers.JsonOptions);
        result!.Data!.IsFollowing.Should().BeTrue();
    }

    [Fact]
    public async Task FollowUser_Self_Returns400()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("selfuser1", "selfuser1@test.com");
        SetBearer(token);

        var resp = await _client.PostAsync($"/api/v1/users/{userId}/follow", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<object>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task FollowUser_AlreadyFollowing_Returns400()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("follower2", "follower2@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("target2", "target2@test.com");

        SetBearer(tokenA);
        await _client.PostAsync($"/api/v1/users/{userBId}/follow", null);

        var resp = await _client.PostAsync($"/api/v1/users/{userBId}/follow", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<object>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UnfollowUser_Existing_Returns200()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("follower3", "follower3@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("target3", "target3@test.com");

        SetBearer(tokenA);
        await _client.PostAsync($"/api/v1/users/{userBId}/follow", null);

        var resp = await _client.DeleteAsync($"/api/v1/users/{userBId}/follow");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnfollowUser_NotFollowing_Returns404()
    {
        var (tokenA, _) = await RegisterAndGetTokenAsync("follower4", "follower4@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("target4", "target4@test.com");

        SetBearer(tokenA);
        var resp = await _client.DeleteAsync($"/api/v1/users/{userBId}/follow");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFollowing_AfterFollow_ContainsUser()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("follower5", "follower5@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("target5", "target5@test.com");

        SetBearer(tokenA);
        await _client.PostAsync($"/api/v1/users/{userBId}/follow", null);

        var resp = await _client.GetAsync($"/api/v1/users/{userAId}/following");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<UserSummaryData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().Contain(u => u.Id == userBId);
    }

    [Fact]
    public async Task GetFollowers_AfterFollow_ContainsFollower()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("follower6", "follower6@test.com");
        var (_, userBId) = await RegisterAndGetTokenAsync("target6", "target6@test.com");

        SetBearer(tokenA);
        await _client.PostAsync($"/api/v1/users/{userBId}/follow", null);

        var resp = await _client.GetAsync($"/api/v1/users/{userBId}/followers");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<UserSummaryData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().Contain(u => u.Id == userAId);
    }

    [Fact]
    public async Task GetFollowStatus_MutualFollow_IsMutualTrue()
    {
        var (tokenA, userAId) = await RegisterAndGetTokenAsync("follower7", "follower7@test.com");
        var (tokenB, userBId) = await RegisterAndGetTokenAsync("target7", "target7@test.com");

        SetBearer(tokenA);
        await _client.PostAsync($"/api/v1/users/{userBId}/follow", null);

        SetBearer(tokenB);
        await _client.PostAsync($"/api/v1/users/{userAId}/follow", null);

        SetBearer(tokenA);
        var resp = await _client.GetAsync($"/api/v1/users/{userBId}/follow-status");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<FollowStatusData>>(body, TestHelpers.JsonOptions);
        result!.Data!.IsMutual.Should().BeTrue();
    }

    // local response shapes
    private record AuthData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username);
    private record FollowStatusData(bool IsFollowing, bool IsFollowedBy, bool IsMutual, int FollowersCount, int FollowingCount);
    private record UserSummaryData(Guid Id, string Username, int Level, bool IsFollowing);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
