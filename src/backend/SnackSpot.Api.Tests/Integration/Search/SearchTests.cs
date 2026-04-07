using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Tests.Integration.Search;

public class SearchTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public SearchTests()
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

    private async Task<(Guid snackId, Guid categoryId)> SeedSnackAsync(
        string snackName, decimal avgRating = 3.0m, int totalReviews = 0)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SnackSpotDbContext>();
        var category = new Category { Name = "SearchCat_" + Guid.NewGuid() };
        var store = new Store { Name = "SearchStore_" + Guid.NewGuid(), Latitude = 0, Longitude = 0, CreatedByUserId = Guid.NewGuid() };
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
        return (snack.Id, category.Id);
    }

    // -- tests --

    [Fact]
    public async Task Search_ByKeyword_Returns200WithResults()
    {
        var uniqueName = "UniqueSearchSnack_" + Guid.NewGuid().ToString("N")[..8];
        await SeedSnackAsync(uniqueName);

        var resp = await _client.GetAsync($"/api/v1/snacks/search?q={uniqueName}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<SnackData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().Contain(s => s.Name == uniqueName);
    }

    [Fact]
    public async Task Search_ByCategoryId_FiltersCorrectly()
    {
        var (snackId, categoryId) = await SeedSnackAsync("CatFilterSnack_" + Guid.NewGuid());
        await SeedSnackAsync("OtherSnack_" + Guid.NewGuid()); // different category

        var resp = await _client.GetAsync($"/api/v1/snacks/search?categoryId={categoryId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<SnackData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().AllSatisfy(s => s.CategoryId.Should().Be(categoryId));
        result.Data.Items.Should().Contain(s => s.Id == snackId);
    }

    [Fact]
    public async Task Search_ByMinRating_FiltersCorrectly()
    {
        await SeedSnackAsync("HighRated_" + Guid.NewGuid(), avgRating: 5.0m);
        await SeedSnackAsync("LowRated_" + Guid.NewGuid(), avgRating: 2.0m);

        var resp = await _client.GetAsync("/api/v1/snacks/search?minRating=5");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<SnackData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().AllSatisfy(s => s.AverageRating.Should().BeGreaterThanOrEqualTo(5m));
    }

    [Fact]
    public async Task Search_SortByRating_OrderedCorrectly()
    {
        await SeedSnackAsync("Sort1_" + Guid.NewGuid(), avgRating: 5.0m);
        await SeedSnackAsync("Sort2_" + Guid.NewGuid(), avgRating: 2.0m);
        await SeedSnackAsync("Sort3_" + Guid.NewGuid(), avgRating: 4.0m);

        var resp = await _client.GetAsync("/api/v1/snacks/search?sort=rating");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<SnackData>>>(body, TestHelpers.JsonOptions);
        var items = result!.Data!.Items;
        items.Count.Should().BeGreaterThanOrEqualTo(2);
        items[0].AverageRating.Should().BeGreaterThanOrEqualTo(items[1].AverageRating);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyPage()
    {
        var resp = await _client.GetAsync("/api/v1/snacks/search?q=ThisSnackDefinitelyDoesNotExist99999");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<PagedData<SnackData>>>(body, TestHelpers.JsonOptions);
        result!.Data!.Items.Should().BeEmpty();
    }

    // local shapes
    private record SnackData(Guid Id, string Name, Guid CategoryId, decimal AverageRating);
    private record PagedData<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
}
