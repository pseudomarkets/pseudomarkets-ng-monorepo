using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.TransactionProcessing.Persistence.Database;

namespace PseudoMarkets.TransactionProcessing.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionProcessingPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TransactionProcessingDb");

        services.AddDbContext<TransactionProcessingDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
