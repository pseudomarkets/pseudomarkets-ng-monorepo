using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;
using PseudoMarkets.ReferenceData.TradingInstruments.Persistence.Repositories;
using PseudoMarkets.Shared.Entities.Database;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingInstrumentsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PseudoMarketsDb");

        services.AddDbContext<PseudoMarketsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgresOptions => postgresOptions.MigrationsAssembly(typeof(PseudoMarketsDbContext).Assembly.FullName)));

        services.AddScoped<ITradingInstrumentRepository, TradingInstrumentRepository>();

        return services;
    }
}
