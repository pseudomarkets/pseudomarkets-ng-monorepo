using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PseudoMarkets.Platform.Batch.Core.DependencyInjection;
using PseudoMarkets.Platform.Batch.Core.Interfaces;
using PseudoMarkets.Platform.Batch.Host.DependencyInjection;
using PseudoMarkets.Platform.Batch.Host.Jobs;
using Shouldly;

namespace PseudoMarkets.Platform.Batch.Tests.Host;

[TestFixture]
public sealed class QueuedOrderExecutionJobRegistrationTests
{
    [Test]
    public void AddQueuedOrderExecutionBatchProcessing_ShouldRegisterQueuedOrderExecutionJobDefinition()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                KeyValuePair.Create<string, string?>("QueuedOrderExecution:IdentityServerBaseUrl", "http://localhost:5051"),
                KeyValuePair.Create<string, string?>("QueuedOrderExecution:OrderExecutionBaseUrl", "http://localhost:8084"),
                KeyValuePair.Create<string, string?>("QueuedOrderExecution:SystemAccountLoginId", "system-user"),
                KeyValuePair.Create<string, string?>("QueuedOrderExecution:SystemAccountPassword", "system-password")
            ])
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPseudoMarketsBatchCore(configuration);
        services.AddQueuedOrderExecutionBatchProcessing(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IBatchJobRegistry>();
        var definition = registry.GetRequiredDefinition(QueuedOrderExecutionJob.JobName);

        definition.JobType.ShouldBe(typeof(QueuedOrderExecutionJob));
        definition.DefaultCronExpression.ShouldBe(QueuedOrderExecutionJob.DefaultCronExpression);
        definition.DefaultQueue.ShouldBe("default");
        definition.DisableConcurrentExecution.ShouldBeTrue();
        definition.DefaultTimeZoneId.ShouldBe(QueuedOrderExecutionJob.TimeZoneId);
    }
}
