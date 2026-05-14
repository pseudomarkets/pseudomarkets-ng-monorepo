using System.Text.Json.Serialization;

namespace PseudoMarkets.MarketData.Providers.Models;

internal sealed class YahooFinanceChartResponse
{
    [JsonPropertyName("chart")]
    public YahooFinanceChartContainer? Chart { get; init; }
}

internal sealed class YahooFinanceChartContainer
{
    [JsonPropertyName("result")]
    public IReadOnlyList<YahooFinanceChartResult> Result { get; init; } = [];
}

internal sealed class YahooFinanceChartResult
{
    [JsonPropertyName("meta")]
    public YahooFinanceChartMeta? Meta { get; init; }
}

internal sealed class YahooFinanceChartMeta
{
    [JsonPropertyName("regularMarketPrice")]
    public decimal? RegularMarketPrice { get; init; }
}
