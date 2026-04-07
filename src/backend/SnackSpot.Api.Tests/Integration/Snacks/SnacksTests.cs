using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Tests.Integration.Snacks;

public class SnacksTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public SnacksTests()
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

    private async Task<string> RegisterAndGetTokenAsync(string username, string email)
    {
        var regResp = await _client.PostAsync("/api/v1/auth/register", TestHelpers.JsonContent(new
        {
            Username = username, Email = email, Password = "Password123!"
        }));
        regResp.EnsureSuccessStatusCode();

        var regBody = await regResp.Content.ReadAsStringAsync();
        var regResult = JsonSerializer.Deserialize<ApiResponseWrapper<AuthResponseData>>(regBody, TestHelpers.JsonOptions)!;
        return regResult.Data!.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    /// <summary>Seed a category + store directly in the InMemory DB and return their IDs.</summary>
    private async Task<(Guid categoryId, Guid storeId)> SeedCategoryAndStoreAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnackSpotDbContext>();

        var category = new Category { Name = "TestCategory_" + Guid.NewGuid() };
        var store = new Store { Name = "TestStore_" + Guid.NewGuid(), Latitude = 0, Longitude = 0, CreatedByUserId = Guid.NewGuid() };

        db.Categories.Add(category);
        db.Stores.Add(store);
        await db.SaveChangesAsync();

        return (category.Id, store.Id);
    }

    // -- tests --

    [Fact]
    public async Task GetSnacks_ReturnsPagedList()
    {
        var response = await _client.GetAsync("/api/v1/snacks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<SnackData>>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSnack_ValidRequest_Returns201()
    {
        var (catId, storeId) = await SeedCategoryAndStoreAsync();
        var token = await RegisterAndGetTokenAsync("snackuser1", "snackuser1@test.com");
        SetBearer(token);

        var response = await _client.PostAsync("/api/v1/snacks", TestHelpers.JsonContent(new
        {
            Name = "Salt & Vinegar Chips",
            CategoryId = catId,
            StoreId = storeId,
            Price = 3.50m,
            ImageUrls = new[] { "https://cdn.test/chip.webp" },
            Tags = new[] { "salty", "crunchy" }
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<SnackData>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Salt & Vinegar Chips");
        result.Data.Images.Should().HaveCount(1);
        result.Data.Tags.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateSnack_InvalidCategory_Returns404()
    {
        var (_, storeId) = await SeedCategoryAndStoreAsync();
        var token = await RegisterAndGetTokenAsync("snackuser2", "snackuser2@test.com");
        SetBearer(token);

        var response = await _client.PostAsync("/api/v1/snacks", TestHelpers.JsonContent(new
        {
            Name = "Mystery Snack",
            CategoryId = Guid.NewGuid(), // non-existent
            StoreId = storeId
        }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSnack_ById_ReturnsWithImagesAndTags()
    {
        var (catId, storeId) = await SeedCategoryAndStoreAsync();
        var token = await RegisterAndGetTokenAsync("snackuser3", "snackuser3@test.com");
        SetBearer(token);

        var createResp = await _client.PostAsync("/api/v1/snacks", TestHelpers.JsonContent(new
        {
            Name = "Wasabi Peas",
            CategoryId = catId,
            StoreId = storeId,
            ImageUrls = new[] { "https://cdn.test/peas.webp" },
            Tags = new[] { "spicy" }
        }));
        createResp.EnsureSuccessStatusCode();
        var createBody = await createResp.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ApiResponseWrapper<SnackData>>(createBody, TestHelpers.JsonOptions)!;
        var snackId = created.Data!.Id;

        var response = await _client.GetAsync($"/api/v1/snacks/{snackId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<SnackData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Images.Should().HaveCount(1);
        result.Data.Tags.Should().HaveCount(1).And.Contain("spicy");
    }

    [Fact]
    public async Task UpdateSnack_ReplacesImagesAndTags()
    {
        var (catId, storeId) = await SeedCategoryAndStoreAsync();
        var token = await RegisterAndGetTokenAsync("snackuser4", "snackuser4@test.com");
        SetBearer(token);

        var createResp = await _client.PostAsync("/api/v1/snacks", TestHelpers.JsonContent(new
        {
            Name = "Original Pringle",
            CategoryId = catId,
            StoreId = storeId,
            ImageUrls = new[] { "https://cdn.test/old.webp" },
            Tags = new[] { "old-tag" }
        }));
        createResp.EnsureSuccessStatusCode();
        var created = JsonSerializer.Deserialize<ApiResponseWrapper<SnackData>>(
            await createResp.Content.ReadAsStringAsync(), TestHelpers.JsonOptions)!;
        var snackId = created.Data!.Id;

        var updateResp = await _client.PutAsync($"/api/v1/snacks/{snackId}", TestHelpers.JsonContent(new
        {
            ImageUrls = new[] { "https://cdn.test/new1.webp", "https://cdn.test/new2.webp" },
            Tags = new[] { "new-tag-1", "new-tag-2" }
        }));

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<SnackData>>(body, TestHelpers.JsonOptions);
        result!.Data!.Images.Should().HaveCount(2);
        result.Data.Tags.Should().HaveCount(2).And.Contain("new-tag-1");
    }

    [Fact]
    public async Task DeleteSnack_Owner_Returns200()
    {
        var (catId, storeId) = await SeedCategoryAndStoreAsync();
        var token = await RegisterAndGetTokenAsync("snackuser5", "snackuser5@test.com");
        SetBearer(token);

        var createResp = await _client.PostAsync("/api/v1/snacks", TestHelpers.JsonContent(new
        {
            Name = "SnackToDelete",
            CategoryId = catId,
            StoreId = storeId
        }));
        createResp.EnsureSuccessStatusCode();
        var created = JsonSerializer.Deserialize<ApiResponseWrapper<SnackData>>(
            await createResp.Content.ReadAsStringAsync(), TestHelpers.JsonOptions)!;
        var snackId = created.Data!.Id;

        var deleteResp = await _client.DeleteAsync($"/api/v1/snacks/{snackId}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await deleteResp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<object>>(body, TestHelpers.JsonOptions);
        result!.Success.Should().BeTrue();
    }

    // local response shapes
    private record AuthResponseData(string AccessToken, UserData User);
    private record UserData(Guid Id, string Username, string Email);
    private record SnackData(Guid Id, string Name, IReadOnlyList<ImageData> Images, IReadOnlyList<string> Tags);
    private record ImageData(Guid Id, string ImageUrl, int DisplayOrder);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
