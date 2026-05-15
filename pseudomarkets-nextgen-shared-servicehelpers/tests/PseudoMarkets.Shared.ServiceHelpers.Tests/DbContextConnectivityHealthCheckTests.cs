using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using PseudoMarkets.Shared.Entities.Database;
using Shouldly;

namespace PseudoMarkets.Shared.ServiceHelpers.Tests;

[TestFixture]
public class DbContextConnectivityHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenDatabaseIsReachable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<PseudoMarketsDbContext>(options => options.UseSqlite(connection));

        await using var serviceProvider = services.BuildServiceProvider();
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PseudoMarketsDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        var sut = new DbContextConnectivityHealthCheck<PseudoMarketsDbContext>(
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("PostgreSQL connection is available.");
    }
}
