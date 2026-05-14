namespace PseudoMarkets.MarketData.Core.Configuration;

public class FinnHubConfiguration
{
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";
    public string ApiKey { get; set; } = string.Empty;
}
