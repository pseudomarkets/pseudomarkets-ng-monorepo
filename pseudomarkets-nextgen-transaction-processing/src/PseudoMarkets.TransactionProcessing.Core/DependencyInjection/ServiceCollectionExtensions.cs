using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;
using PseudoMarkets.TransactionProcessing.Core.Services;

namespace PseudoMarkets.TransactionProcessing.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionProcessingCore(this IServiceCollection services)
    {
        services.AddScoped<ITransactionDescriptionService, TransactionDescriptionService>();
        services.AddScoped<ITradeTransactionPostingService, TradeTransactionPostingService>();
        services.AddScoped<ICashMovementPostingService, CashMovementPostingService>();
        services.AddScoped<IVoidTransactionService, VoidTransactionService>();

        return services;
    }
}
