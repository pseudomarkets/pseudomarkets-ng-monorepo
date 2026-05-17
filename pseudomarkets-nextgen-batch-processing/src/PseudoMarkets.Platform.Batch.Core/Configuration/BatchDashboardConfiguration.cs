namespace PseudoMarkets.Platform.Batch.Core.Configuration;

public sealed class BatchDashboardConfiguration
{
    public bool Enabled { get; set; } = true;
    public string Path { get; set; } = "/hangfire";
    public bool ReadOnly { get; set; }
}
