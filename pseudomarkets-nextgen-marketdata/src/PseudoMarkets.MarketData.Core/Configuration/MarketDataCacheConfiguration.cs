namespace PseudoMarkets.MarketData.Core.Configuration;

public class MarketDataCacheConfiguration
{
    public int QuoteTtlSeconds { get; set; }
    public int DetailedQuoteTtlSeconds { get; set; }
    public int IndicesTtlSeconds { get; set; }
}
