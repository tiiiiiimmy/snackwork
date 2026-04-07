using System.Net;
using System.Text.Json;
using FluentAssertions;
using SnackSpot.Api.Models.DTOs.Auth;

namespace SnackSpot.Api.Tests.Integration.Auth;

public class LoginTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LoginTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static StringContent JsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj), System.Text.Encoding.UTF8, "application/json");

    private async Task RegisterUser(string username, string email, string password)
    {
        var request = new { Username = username, Email = email, Password = password };
        var response = await _client.PostAsync("/api/v1/auth/register", JsonContent(request));
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        await RegisterUser("loginuser1", "loginuser1@example.com", "Password123");

        var loginRequest = new { Email = "loginuser1@example.com", Password = "Password123" };
        var response = await _client.PostAsync("/api/v1/auth/login", JsonContent(loginRequest));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<AuthResponse>>(body, JsonOptions);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await RegisterUser("loginuser2", "loginuser2@example.com", "Password123");

        var loginRequest = new { Email = "loginuser2@example.com", Password = "WrongPassword!" };
        var response = await _client.PostAsync("/api/v1/auth/login", JsonContent(loginRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401()
    {
        var loginRequest = new { Email = "nonexistent@example.com", Password = "Password123" };
        var response = await _client.PostAsync("/api/v1/auth/login", JsonContent(loginRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
