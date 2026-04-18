using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.MarketData.Core.Interfaces;
using PseudoMarkets.MarketData.Providers.Implementations;

namespace PseudoMarkets.MarketData.Providers.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarketDataProviders(this IServiceCollection services)
    {
        services.AddHttpClient<IMarketDataProvider, TwelveDataMarketDataProvider>();

        return services;
    }
}
