namespace PseudoMarkets.MarketData.Service.Contracts;

public class ServiceInfoResponse
{
    public required string Name { get; init; }
    public required string Environment { get; init; }
    public string Version { get; init; } = string.Empty;
}
