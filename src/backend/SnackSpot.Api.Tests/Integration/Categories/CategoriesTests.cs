using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;

namespace SnackSpot.Api.Tests.Integration.Categories;

public class CategoriesTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public CategoriesTests()
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

    private async Task<string> RegisterAndGetTokenAsync(string username, string email, int level = 1)
    {
        var regResp = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(new
        {
            Username = username, Email = email, Password = "Password123!"
        }));
        regResp.EnsureSuccessStatusCode();

        var regBody = await regResp.Content.ReadAsStringAsync();
        var regResult = JsonSerializer.Deserialize<ApiResponseWrapper<AuthResponseData>>(regBody, TestHelpers.JsonOptions)!;
        var userId = regResult.Data!.User.Id;

        if (level > 1)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SnackSpotDbContext>();
            var user = await db.Users.FindAsync(userId);
            user!.Level = level;
            await db.SaveChangesAsync();
        }

        // Login to get fresh token (still valid regardless of level changes — service reads level from DB)
        return regResult.Data.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // -- tests --

    [Fact]
    public async Task GetCategories_ReturnsPagedList()
    {
        var response = await _client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<CategoryData>>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCategory_ValidRequest_Returns201()
    {
        var token = await RegisterAndGetTokenAsync("catuser1", "catuser1@test.com", level: 2);
        SetBearer(token);

        var response = await _client.PostAsync("/api/v1/categories",
            TestHelpers.JsonContent(new { Name = "Chips", Description = "Crunchy snacks" }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<CategoryData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Chips");
    }

    [Fact]
    public async Task CreateCategory_InsufficientLevel_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("catuser2", "catuser2@test.com", level: 1);
        SetBearer(token);

        var response = await _client.PostAsync("/api/v1/categories",
            TestHelpers.JsonContent(new { Name = "Candy" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<CategoryData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("catuser3", "catuser3@test.com", level: 2);
        SetBearer(token);

        var first = await _client.PostAsync("/api/v1/categories",
            TestHelpers.JsonContent(new { Name = "Crackers" }));
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsync("/api/v1/categories",
            TestHelpers.JsonContent(new { Name = "Crackers" }));

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await second.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<CategoryData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCategory_NotOwner_Returns401()
    {
        // User A creates a category
        var tokenA = await RegisterAndGetTokenAsync("catuserA", "catuserA@test.com", level: 2);
        SetBearer(tokenA);

        var createResp = await _client.PostAsync("/api/v1/categories",
            TestHelpers.JsonContent(new { Name = "Popcorn" }));
        createResp.EnsureSuccessStatusCode();
        var createBody = await createResp.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ApiResponseWrapper<CategoryData>>(createBody, TestHelpers.JsonOptions);
        var categoryId = created!.Data!.Id;

        // User B tries to update
        var tokenB = await RegisterAndGetTokenAsync("catuserB", "catuserB@test.com", level: 2);
        SetBearer(tokenB);

        var updateResp = await _client.PutAsync($"/api/v1/categories/{categoryId}",
            TestHelpers.JsonContent(new { Name = "Popcorn Updated" }));

        updateResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteCategory_Owner_Returns200()
    {
        var token = await RegisterAndGetTokenAsync("catuser5", "catuser5@test.com", level: 2);
        SetBearer(token);

        var createResp = await _client.PostAsync("/api/v1/categories",
            TestHelpers.JsonContent(new { Name = "NachosToDelete" }));
        createResp.EnsureSuccessStatusCode();
        var createBody = await createResp.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ApiResponseWrapper<CategoryData>>(createBody, TestHelpers.JsonOptions);
        var categoryId = created!.Data!.Id;

        var deleteResp = await _client.DeleteAsync($"/api/v1/categories/{categoryId}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await deleteResp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<object>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
    }

    // local response shapes
    private record AuthResponseData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username, string Email, int Level);
    private record CategoryData(Guid Id, string Name, string? Description, string? Icon, Guid? CreatedByUserId, DateTime CreatedAt);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
