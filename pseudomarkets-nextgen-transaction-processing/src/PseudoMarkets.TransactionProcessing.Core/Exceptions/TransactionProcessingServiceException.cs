namespace PseudoMarkets.TransactionProcessing.Core.Exceptions;

public class TransactionProcessingServiceException : Exception
{
    public TransactionProcessingServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
