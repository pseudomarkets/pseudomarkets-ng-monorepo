using PseudoMarkets.Platform.Batch.Core.Interfaces;
using PseudoMarkets.Platform.Batch.Core.Models;

namespace PseudoMarkets.Platform.Batch.Core.Services;

public sealed class BatchJobRegistry : IBatchJobRegistry
{
    private readonly IReadOnlyDictionary<string, BatchJobDefinition> _definitions;

    public BatchJobRegistry(IEnumerable<BatchJobDefinition> definitions)
    {
        var definitionDictionary = new Dictionary<string, BatchJobDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            if (definitionDictionary.ContainsKey(definition.JobName))
            {
                throw new InvalidOperationException($"A batch job named '{definition.JobName}' is already registered.");
            }

            definitionDictionary[definition.JobName] = definition;
        }

        _definitions = definitionDictionary;
    }

    public IReadOnlyCollection<BatchJobDefinition> Definitions => _definitions.Values.ToArray();

    public BatchJobDefinition GetRequiredDefinition(string jobName)
    {
        if (_definitions.TryGetValue(jobName, out var definition))
        {
            return definition;
        }

        throw new InvalidOperationException($"No batch job named '{jobName}' is registered.");
    }
}
