using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PseudoMarkets.BalancesAndPositions.Core.Clients;
using PseudoMarkets.BalancesAndPositions.Core.Configuration;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.BalancesAndPositions.Core.Models;
using PseudoMarkets.BalancesAndPositions.Core.Services;
using PseudoMarkets.Shared.Authorization.Configuration;

namespace PseudoMarkets.BalancesAndPositions.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBalancesAndPositionsCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BalancesAndPositionsConfiguration>(configuration.GetSection(BalancesAndPositionsConfiguration.SectionName));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISystemTokenProvider, SystemTokenProvider>();
        services.AddScoped<IBalanceQueryService, BalanceQueryService>();
        services.AddScoped<IPositionQueryService, PositionQueryService>();

        services.AddHttpClient("BalancesAndPositionsIdentityServer", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<IdentityAuthorizationConfiguration>>().Value;
            if (Uri.TryCreate(options.IdentityServerBaseUrl, UriKind.Absolute, out var baseAddress))
            {
                client.BaseAddress = baseAddress;
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 10);
        });

        services.AddHttpClient<IMarketDataQuoteClient, MarketDataQuoteClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BalancesAndPositionsConfiguration>>().Value;
            if (Uri.TryCreate(options.MarketDataBaseUrl, UriKind.Absolute, out var baseAddress))
            {
                client.BaseAddress = baseAddress;
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 10);
        });

        return services;
    }
}
