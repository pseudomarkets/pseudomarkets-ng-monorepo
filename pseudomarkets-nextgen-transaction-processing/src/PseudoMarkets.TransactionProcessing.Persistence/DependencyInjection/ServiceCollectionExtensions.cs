using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.Shared.Entities.Database;

namespace PseudoMarkets.TransactionProcessing.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionProcessingPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PseudoMarketsDb");

        services.AddDbContext<PseudoMarketsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgresOptions => postgresOptions.MigrationsAssembly(typeof(PseudoMarketsDbContext).Assembly.FullName)));

        return services;
    }
}
