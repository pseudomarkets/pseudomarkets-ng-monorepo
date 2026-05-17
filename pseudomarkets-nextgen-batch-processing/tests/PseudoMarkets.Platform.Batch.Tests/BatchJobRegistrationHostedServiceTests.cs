using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PseudoMarkets.Platform.Batch.Core.Configuration;
using PseudoMarkets.Platform.Batch.Core.Models;
using PseudoMarkets.Platform.Batch.Core.Services;
using Shouldly;

namespace PseudoMarkets.Platform.Batch.Tests;

[TestFixture]
public sealed class BatchJobRegistrationHostedServiceTests
{
    [Test]
    public async Task StartAsync_ShouldRegisterEnabledRecurringJobs()
    {
        var recurringJobManager = new Mock<IRecurringJobManager>();
        var registry = new BatchJobRegistry(
        [
            new BatchJobDefinition(typeof(FakeBatchJob), "job-1", "0 * * * *", "default", true, "UTC")
        ]);
        var resolver = new BatchJobConfigurationResolver(Options.Create(new BatchProcessingConfiguration()));
        var sut = new BatchJobRegistrationHostedService(
            recurringJobManager.Object,
            registry,
            resolver,
            Options.Create(new BatchProcessingConfiguration()),
            NullLogger<BatchJobRegistrationHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        recurringJobManager.Verify(
            manager => manager.AddOrUpdate(
                "job-1",
                It.IsAny<Job>(),
                "0 * * * *",
#pragma warning disable CS0618
                It.Is<RecurringJobOptions>(options => options.QueueName == "default")),
#pragma warning restore CS0618
            Times.Once);
    }

    [Test]
    public async Task StartAsync_ShouldRemoveDisabledRecurringJobs()
    {
        var recurringJobManager = new Mock<IRecurringJobManager>();
        var registry = new BatchJobRegistry(
        [
            new BatchJobDefinition(typeof(FakeBatchJob), "job-1", "0 * * * *", "default", true, "UTC")
        ]);
        var options = Options.Create(new BatchProcessingConfiguration
        {
            Jobs = new Dictionary<string, BatchJobConfiguration>(StringComparer.OrdinalIgnoreCase)
            {
                ["job-1"] = new() { Enabled = false }
            }
        });
        var resolver = new BatchJobConfigurationResolver(options);
        var sut = new BatchJobRegistrationHostedService(
            recurringJobManager.Object,
            registry,
            resolver,
            options,
            NullLogger<BatchJobRegistrationHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        recurringJobManager.Verify(manager => manager.RemoveIfExists("job-1"), Times.Once);
        recurringJobManager.Verify(
            manager => manager.AddOrUpdate(
                It.IsAny<string>(),
                It.IsAny<Job>(),
                It.IsAny<string>(),
                It.IsAny<RecurringJobOptions>()),
            Times.Never);
    }
}
