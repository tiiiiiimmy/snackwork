using System.Net;
using System.Text.Json;
using FluentAssertions;
using SnackSpot.Api.Models.DTOs.Auth;

namespace SnackSpot.Api.Tests.Integration.Auth;

public class RegisterTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public RegisterTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns200()
    {
        var request = new { Username = "testuser1", Email = "test1@example.com", Password = "Password123" };

        var response = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<AuthResponse>>(body, TestHelpers.JsonOptions);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.User.Should().NotBeNull();
        result.Data.User.Username.Should().Be("testuser1");
        result.Data.User.Email.Should().Be("test1@example.com");
        result.Data.User.Level.Should().Be(1);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var request1 = new { Username = "uniqueuser1", Email = "duplicate@example.com", Password = "Password123" };
        var request2 = new { Username = "uniqueuser2", Email = "duplicate@example.com", Password = "Password123" };

        var response1 = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(request1));
        response1.EnsureSuccessStatusCode();
        var response = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(request2));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<AuthResponse>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns400()
    {
        var request1 = new { Username = "sameusername", Email = "email1@example.com", Password = "Password123" };
        var request2 = new { Username = "sameusername", Email = "email2@example.com", Password = "Password123" };

        var response1 = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(request1));
        response1.EnsureSuccessStatusCode();
        var response = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(request2));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<AuthResponse>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var request = new { Username = "validuser", Email = "not-an-email", Password = "Password123" };

        var response = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<AuthResponse>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var request = new { Username = "validuser2", Email = "valid2@example.com", Password = "short" };

        var response = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<AuthResponse>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }
}
