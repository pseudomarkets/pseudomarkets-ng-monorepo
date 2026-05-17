namespace PseudoMarkets.Platform.Batch.Core.Configuration;

public sealed class BatchServerConfiguration
{
    public int WorkerCount { get; set; } = 5;
    public string[] Queues { get; set; } = ["default"];
    public string ServerName { get; set; } = "pseudomarkets-platform-batch-host";
}
