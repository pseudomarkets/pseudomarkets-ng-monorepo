using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.MarketData.Cache.Implementations;
using PseudoMarkets.MarketData.Core.Interfaces;

namespace PseudoMarkets.MarketData.Cache.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarketDataCache(this IServiceCollection services)
    {
        services.AddSingleton<IMarketDataCache, AerospikeMarketDataCache>();

        return services;
    }
}
