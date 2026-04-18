using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.Shared.Authorization.Clients;
using PseudoMarkets.Shared.Authorization.Configuration;
using PseudoMarkets.Shared.Authorization.Interfaces;

namespace PseudoMarkets.Shared.Authorization.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPseudoMarketsSharedAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(IdentityAuthorizationConfiguration.SectionName);
        services.Configure<IdentityAuthorizationConfiguration>(section);

        services.AddHttpClient<IIdentityAuthorizationClient, IdentityAuthorizationClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityAuthorizationConfiguration>>()
                .Value;

            if (Uri.TryCreate(options.IdentityServerBaseUrl, UriKind.Absolute, out var baseAddress))
            {
                client.BaseAddress = baseAddress;
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 10);
        });

        return services;
    }
}
