using System.Text.Json;

namespace SnackSpot.Api.Tests;

internal static class TestHelpers
{
    internal static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal static StringContent JsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj), System.Text.Encoding.UTF8, "application/json");
}
