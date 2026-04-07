using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;

namespace SnackSpot.Api.Tests.Integration.Stores;

public class StoresTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public StoresTests()
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

        return regResult.Data.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static object ValidStoreRequest(string name = "Countdown Auckland") => new
    {
        Name = name,
        Address = "1 Queen St, Auckland",
        Latitude = -36.8485m,
        Longitude = 174.7633m,
        Description = "A supermarket"
    };

    // -- tests --

    [Fact]
    public async Task GetStores_ReturnsPagedList()
    {
        var response = await _client.GetAsync("/api/v1/stores");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<StoreData>>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateStore_ValidRequest_Returns201()
    {
        var token = await RegisterAndGetTokenAsync("storeuser1", "storeuser1@test.com", level: 7);
        SetBearer(token);

        var response = await _client.PostAsync("/api/v1/stores",
            TestHelpers.JsonContent(ValidStoreRequest("New World Ponsonby")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<StoreData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("New World Ponsonby");
    }

    [Fact]
    public async Task CreateStore_InsufficientLevel_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("storeuser2", "storeuser2@test.com", level: 1);
        SetBearer(token);

        var response = await _client.PostAsync("/api/v1/stores",
            TestHelpers.JsonContent(ValidStoreRequest()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<StoreData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStore_NotOwner_Returns401()
    {
        // User A creates
        var tokenA = await RegisterAndGetTokenAsync("storeuserA", "storeuserA@test.com", level: 7);
        SetBearer(tokenA);

        var createResp = await _client.PostAsync("/api/v1/stores",
            TestHelpers.JsonContent(ValidStoreRequest("Pak'nSave Mangere")));
        createResp.EnsureSuccessStatusCode();
        var createBody = await createResp.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ApiResponseWrapper<StoreData>>(createBody, TestHelpers.JsonOptions);
        var storeId = created!.Data!.Id;

        // User B tries to update
        var tokenB = await RegisterAndGetTokenAsync("storeuserB", "storeuserB@test.com", level: 7);
        SetBearer(tokenB);

        var updateResp = await _client.PutAsync($"/api/v1/stores/{storeId}",
            TestHelpers.JsonContent(new { Name = "Hacked Store Name" }));

        updateResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteStore_Owner_Returns200()
    {
        var token = await RegisterAndGetTokenAsync("storeuser5", "storeuser5@test.com", level: 7);
        SetBearer(token);

        var createResp = await _client.PostAsync("/api/v1/stores",
            TestHelpers.JsonContent(ValidStoreRequest("StoreToDelete")));
        createResp.EnsureSuccessStatusCode();
        var createBody = await createResp.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ApiResponseWrapper<StoreData>>(createBody, TestHelpers.JsonOptions);
        var storeId = created!.Data!.Id;

        var deleteResp = await _client.DeleteAsync($"/api/v1/stores/{storeId}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await deleteResp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<object>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
    }

    // local response shapes
    private record AuthResponseData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username, string Email, int Level);
    private record StoreData(Guid Id, string Name, string? Address, decimal Latitude, decimal Longitude, Guid CreatedByUserId);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
