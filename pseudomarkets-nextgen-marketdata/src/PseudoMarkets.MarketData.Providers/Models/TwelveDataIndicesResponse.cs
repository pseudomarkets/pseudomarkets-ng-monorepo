using System.Text.Json.Serialization;

namespace PseudoMarkets.MarketData.Providers.Models;

public class TwelveDataIndicesResponse
{
    [JsonPropertyName("spx")]
    public TwelveDataIndexSeries? Spx { get; init; }

    [JsonPropertyName("ixic")]
    public TwelveDataIndexSeries? Ixic { get; init; }

    [JsonPropertyName("dji")]
    public TwelveDataIndexSeries? Dow { get; init; }
}

public class TwelveDataIndexSeries
{
    [JsonPropertyName("values")]
    public IReadOnlyList<TwelveDataIndexValue> Values { get; init; } = Array.Empty<TwelveDataIndexValue>();
}

public class TwelveDataIndexValue
{
    [JsonPropertyName("close")]
    public string? Close { get; init; }
}
