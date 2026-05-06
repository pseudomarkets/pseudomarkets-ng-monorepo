namespace PseudoMarkets.OrderExecution.Core.Configuration;

public sealed class OrderExecutionConfiguration
{
    public const string SectionName = "OrderExecution";

    public string IdentityServerBaseUrl { get; init; } = string.Empty;
    public string SystemAccountLoginId { get; init; } = string.Empty;
    public string SystemAccountPassword { get; init; } = string.Empty;
    public string TradingInstrumentsBaseUrl { get; init; } = string.Empty;
    public string MarketDataBaseUrl { get; init; } = string.Empty;
    public string TransactionProcessingBaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 10;
    public int TokenRefreshBufferSeconds { get; init; } = 60;
}
