using PseudoMarkets.Platform.Batch.Core.Models;
using PseudoMarkets.Platform.Batch.Core.Services;
using Shouldly;

namespace PseudoMarkets.Platform.Batch.Tests;

[TestFixture]
public sealed class BatchJobRegistryTests
{
    [Test]
    public void Constructor_ShouldRejectDuplicateJobNames()
    {
        var duplicateDefinitions = new[]
        {
            new BatchJobDefinition(typeof(FakeBatchJob), "job-1", "* * * * *", "default", true, "UTC"),
            new BatchJobDefinition(typeof(SecondFakeBatchJob), "job-1", "* * * * *", "default", true, "UTC")
        };

        Should.Throw<InvalidOperationException>(() => new BatchJobRegistry(duplicateDefinitions));
    }

    [Test]
    public void GetRequiredDefinition_ShouldReturnRegisteredDefinition()
    {
        var registry = new BatchJobRegistry(
        [
            new BatchJobDefinition(typeof(FakeBatchJob), "job-1", "* * * * *", "critical", true, "UTC")
        ]);

        var definition = registry.GetRequiredDefinition("job-1");

        definition.JobName.ShouldBe("job-1");
        definition.DefaultQueue.ShouldBe("critical");
    }
}
