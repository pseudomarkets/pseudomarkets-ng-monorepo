using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Persistence.Repositories;
using PseudoMarkets.Shared.Entities.Database;

namespace PseudoMarkets.OrderExecution.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderExecutionPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PseudoMarketsDb");

        services.AddDbContext<PseudoMarketsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgresOptions => postgresOptions.MigrationsAssembly(typeof(PseudoMarketsDbContext).Assembly.FullName)));

        services.AddScoped<IOrderExecutionRepository, OrderExecutionRepository>();

        return services;
    }
}
