namespace PseudoMarkets.MarketData.Core.Configuration;

public class AerospikeConfiguration
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string QuoteSetName { get; set; } = string.Empty;
    public string DetailedQuoteSetName { get; set; } = string.Empty;
    public string IndicesSetName { get; set; } = string.Empty;
}
