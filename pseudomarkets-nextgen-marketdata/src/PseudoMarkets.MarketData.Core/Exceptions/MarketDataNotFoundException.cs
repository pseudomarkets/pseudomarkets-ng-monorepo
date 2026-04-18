namespace PseudoMarkets.MarketData.Core.Exceptions;

public class MarketDataNotFoundException : Exception
{
    public MarketDataNotFoundException(string message)
        : base(message)
    {
    }
}
