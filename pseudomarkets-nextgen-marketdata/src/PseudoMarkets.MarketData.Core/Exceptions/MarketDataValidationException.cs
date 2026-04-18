namespace PseudoMarkets.MarketData.Core.Exceptions;

public class MarketDataValidationException : Exception
{
    public MarketDataValidationException(string message)
        : base(message)
    {
    }
}
