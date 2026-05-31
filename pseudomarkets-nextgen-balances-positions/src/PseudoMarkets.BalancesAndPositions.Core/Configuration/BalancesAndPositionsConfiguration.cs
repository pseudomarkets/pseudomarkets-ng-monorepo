namespace PseudoMarkets.BalancesAndPositions.Core.Configuration;

public class BalancesAndPositionsConfiguration
{
    public const string SectionName = "BalancesAndPositions";

    public string SystemAccountLoginId { get; set; } = string.Empty;

    public string SystemAccountPassword { get; set; } = string.Empty;

    public string MarketDataBaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;

    public int TokenRefreshBufferSeconds { get; set; } = 60;
}
