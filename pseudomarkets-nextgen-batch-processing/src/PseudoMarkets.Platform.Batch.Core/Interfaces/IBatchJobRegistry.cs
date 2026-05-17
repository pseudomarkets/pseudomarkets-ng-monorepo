using PseudoMarkets.Platform.Batch.Core.Models;

namespace PseudoMarkets.Platform.Batch.Core.Interfaces;

public interface IBatchJobRegistry
{
    IReadOnlyCollection<BatchJobDefinition> Definitions { get; }
    BatchJobDefinition GetRequiredDefinition(string jobName);
}
