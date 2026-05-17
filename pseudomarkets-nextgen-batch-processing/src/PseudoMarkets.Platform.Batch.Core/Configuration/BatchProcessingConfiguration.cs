namespace PseudoMarkets.Platform.Batch.Core.Configuration;

public sealed class BatchProcessingConfiguration
{
    public const string SectionName = "BatchProcessing";

    public bool Enabled { get; set; } = true;
    public BatchDashboardConfiguration Dashboard { get; set; } = new();
    public BatchServerConfiguration Server { get; set; } = new();
    public BatchStorageConfiguration Storage { get; set; } = new();
    public Dictionary<string, BatchJobConfiguration> Jobs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
