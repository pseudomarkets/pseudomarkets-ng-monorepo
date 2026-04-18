namespace PseudoMarkets.MarketData.Core.Exceptions;

public class MarketDataDependencyException : Exception
{
    public MarketDataDependencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
