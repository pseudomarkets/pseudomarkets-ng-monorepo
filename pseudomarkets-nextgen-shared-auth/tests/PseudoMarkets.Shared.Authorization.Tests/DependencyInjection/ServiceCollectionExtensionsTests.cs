using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Shared.Authorization.Clients;
using PseudoMarkets.Shared.Authorization.Configuration;
using PseudoMarkets.Shared.Authorization.DependencyInjection;
using PseudoMarkets.Shared.Authorization.Interfaces;

namespace PseudoMarkets.Shared.Authorization.Tests.DependencyInjection;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddPseudoMarketsSharedAuthorization_ShouldRegisterTypedClientWithConfiguredBaseAddressAndTimeout()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = CreateConfiguration([
            KeyValuePair.Create<string, string?>("IdentityAuthorization:IdentityServerBaseUrl", "http://localhost:8080"),
            KeyValuePair.Create<string, string?>("IdentityAuthorization:AuthorizeEndpointPath", "/api/identity/authorize"),
            KeyValuePair.Create<string, string?>("IdentityAuthorization:TimeoutSeconds", "15")
        ]);

        services.AddPseudoMarketsSharedAuthorization(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var client = serviceProvider.GetRequiredService<IIdentityAuthorizationClient>()
            .ShouldBeOfType<IdentityAuthorizationClient>();

        var httpClient = GetHttpClient(client);
        httpClient.BaseAddress.ShouldBe(new Uri("http://localhost:8080/"));
        httpClient.Timeout.ShouldBe(TimeSpan.FromSeconds(15));

        var options = serviceProvider.GetRequiredService<IOptions<IdentityAuthorizationConfiguration>>().Value;
        options.IdentityServerBaseUrl.ShouldBe("http://localhost:8080");
        options.AuthorizeEndpointPath.ShouldBe("/api/identity/authorize");
        options.TimeoutSeconds.ShouldBe(15);
    }

    [Test]
    public void AddPseudoMarketsSharedAuthorization_ShouldFallbackToDefaultTimeout_WhenConfiguredTimeoutIsNotPositive()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = CreateConfiguration([
            KeyValuePair.Create<string, string?>("IdentityAuthorization:IdentityServerBaseUrl", "not-a-valid-uri"),
            KeyValuePair.Create<string, string?>("IdentityAuthorization:TimeoutSeconds", "0")
        ]);

        services.AddPseudoMarketsSharedAuthorization(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var client = serviceProvider.GetRequiredService<IIdentityAuthorizationClient>()
            .ShouldBeOfType<IdentityAuthorizationClient>();

        var httpClient = GetHttpClient(client);
        httpClient.BaseAddress.ShouldBeNull();
        httpClient.Timeout.ShouldBe(TimeSpan.FromSeconds(10));

        var options = serviceProvider.GetRequiredService<IOptions<IdentityAuthorizationConfiguration>>().Value;
        options.AuthorizeEndpointPath.ShouldBe("/api/identity/authorize");
        options.TimeoutSeconds.ShouldBe(0);
    }

    private static IConfiguration CreateConfiguration(IEnumerable<KeyValuePair<string, string?>> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static HttpClient GetHttpClient(IdentityAuthorizationClient client)
    {
        return typeof(IdentityAuthorizationClient)
            .GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(client)
            .ShouldBeOfType<HttpClient>();
    }
}
