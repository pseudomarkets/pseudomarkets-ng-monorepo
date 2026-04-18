namespace PseudoMarkets.MarketData.Core.Exceptions;

public class MarketDataServiceException : Exception
{
    public MarketDataServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
