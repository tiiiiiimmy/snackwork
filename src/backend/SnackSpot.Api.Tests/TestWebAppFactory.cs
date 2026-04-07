using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnackSpot.Api.Data;
using SnackSpot.Api.Infrastructure.Storage;

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

            // Replace distributed cache with in-memory for tests (no Redis required)
            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDistributedCache));
            if (cacheDescriptor is not null) services.Remove(cacheDescriptor);
            services.AddDistributedMemoryCache();

            // Replace R2 with mock so tests don't need real R2 credentials
            var r2Descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IR2StorageService));
            if (r2Descriptor is not null)
                services.Remove(r2Descriptor);
            services.AddSingleton<IR2StorageService, MockR2StorageService>();

            // Explicitly reconfigure JWT validation to match what AuthService generates in tests.
            // JWT bearer options are configured at service-registration time in Program.cs and may
            // read appsettings.json before ConfigureAppConfiguration overrides are applied.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("test-super-secret-key-that-is-at-least-32-characters-long"));
                options.TokenValidationParameters.IssuerSigningKey = key;
                options.TokenValidationParameters.ValidIssuer = "SnackSpotTest";
                options.TokenValidationParameters.ValidAudience = "SnackSpotTestClient";
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.ValidateLifetime = true;
            });
        });
    }
}

public record ApiResponseWrapper<T>(bool Success, T? Data, string? Message);

internal class MockR2StorageService : IR2StorageService
{
    public Task<(string uploadUrl, string imageUrl)> GeneratePresignedPutUrlAsync(
        string fileName, string contentType, TimeSpan expiry) =>
        Task.FromResult(("https://upload.test/" + fileName, "https://cdn.test/" + fileName));
}
