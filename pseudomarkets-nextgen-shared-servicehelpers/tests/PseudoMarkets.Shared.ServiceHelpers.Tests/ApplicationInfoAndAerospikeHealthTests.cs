using System.Text.Json;
using Aerospike.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace PseudoMarkets.Shared.ServiceHelpers.Tests;

[TestFixture]
public class ApplicationInfoAndAerospikeHealthTests
{
    [Test]
    public void ApplicationInfoProvider_ShouldReturnMetadata_ForSharedLibraryAssembly()
    {
        var info = ApplicationInfoProvider.GetInfo(typeof(ApplicationInfoProvider).Assembly);

        info.Name.ShouldBe("Pseudo Markets Shared Service Helpers");
        info.Version.ShouldNotBeNullOrWhiteSpace();
        info.BuildTimestamp.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task AerospikeClientHealthCheck_ShouldReturnHealthy_WhenClientIsConnected()
    {
        var client = new Mock<IAerospikeClient>();
        client.SetupGet(x => x.Connected).Returns(true);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(client.Object)
            .BuildServiceProvider();

        var sut = new AerospikeClientHealthCheck(serviceProvider);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("Aerospike client is connected.");
    }

    [Test]
    public async Task AerospikeClientHealthCheck_ShouldReturnUnhealthy_WhenClientIsDisconnected()
    {
        var client = new Mock<IAerospikeClient>();
        client.SetupGet(x => x.Connected).Returns(false);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(client.Object)
            .BuildServiceProvider();

        var sut = new AerospikeClientHealthCheck(serviceProvider);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Aerospike client is not connected.");
    }

    [Test]
    public async Task AerospikeClientHealthCheck_ShouldReturnUnhealthy_WhenSharedClientCannotBeResolved()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var sut = new AerospikeClientHealthCheck(serviceProvider);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Aerospike connectivity check failed.");
    }

    [Test]
    public async Task HealthCheckJsonResponseWriter_ShouldSerializeHealthReport()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["aerospike"] = new(
                    HealthStatus.Healthy,
                    "Aerospike client is connected.",
                    TimeSpan.FromMilliseconds(5),
                    null,
                    new Dictionary<string, object>())
            },
            TimeSpan.FromMilliseconds(5));

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await HealthCheckJsonResponseWriter.WriteAsync(httpContext, report);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var payload = await reader.ReadToEndAsync();
        using var document = JsonDocument.Parse(payload);

        document.RootElement.GetProperty("status").GetString().ShouldBe("Healthy");
        document.RootElement.GetProperty("results").GetProperty("aerospike").GetProperty("status").GetString()
            .ShouldBe("Healthy");
    }
}
