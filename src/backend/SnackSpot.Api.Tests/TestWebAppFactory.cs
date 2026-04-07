using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnackSpot.Api.Data;

namespace SnackSpot.Api.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration to satisfy startup validation
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;",
                ["Jwt:Secret"] = "test-super-secret-key-that-is-at-least-32-characters-long",
                ["Jwt:Issuer"] = "SnackSpotTest",
                ["Jwt:Audience"] = "SnackSpotTestClient",
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL descriptors related to DbContext to avoid MySQL connection attempts
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<SnackSpotDbContext>) ||
                    d.ServiceType == typeof(SnackSpotDbContext) ||
                    d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("DbContextOptions") ||
                    d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("IDbContextOptionsConfiguration"))
                .ToList();

            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Add InMemory DbContext with unique name per factory instance
            var dbName = "TestDb_" + Guid.NewGuid();
            services.AddDbContext<SnackSpotDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        });
    }
}

public record ApiResponseWrapper<T>(bool Success, T? Data, string? Message);
