using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;

namespace SnackSpot.Api.Tests.Integration.Leaderboards;

public class LeaderboardTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public LeaderboardTests()
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
    public async Task GetLeaderboard_Experience_Returns200()
    {
        await RegisterAndGetTokenAsync("lbuser1", "lbuser1@test.com");

        var resp = await _client.GetAsync("/api/v1/leaderboards?type=experience");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<LeaderboardData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Type.Should().Be("experience");
        result.Data.Entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLeaderboard_Contribution_Returns200()
    {
        var resp = await _client.GetAsync("/api/v1/leaderboards?type=contribution");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<LeaderboardData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Type.Should().Be("contribution");
    }

    [Fact]
    public async Task GetMyRank_Returns200WithScore()
    {
        var (token, _) = await RegisterAndGetTokenAsync("lbuser2", "lbuser2@test.com");
        SetBearer(token);

        var resp = await _client.GetAsync("/api/v1/leaderboards/my-rank?type=experience");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<MyRankData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Type.Should().Be("experience");
        result.Data.Score.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetUnlockedFeatures_Level1_Returns200()
    {
        var (_, userId) = await RegisterAndGetTokenAsync("lbuser3", "lbuser3@test.com");

        var resp = await _client.GetAsync($"/api/v1/users/{userId}/unlocked-features");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<UnlockedFeaturesData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Level.Should().Be(1);
        result.Data.Features.Should().NotBeNull();
    }

    // local shapes
    private record AuthData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username);
    private record LeaderboardEntryData(int Rank, Guid UserId, string Username, int Score);
    private record LeaderboardData(string Type, IReadOnlyList<LeaderboardEntryData> Entries, DateTime ComputedAt);
    private record MyRankData(string Type, int? Rank, int Score);
    private record UnlockedFeaturesData(int Level, IReadOnlyList<string> Features);
}
