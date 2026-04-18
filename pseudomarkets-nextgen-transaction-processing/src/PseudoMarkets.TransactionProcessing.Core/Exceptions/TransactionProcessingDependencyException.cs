namespace PseudoMarkets.TransactionProcessing.Core.Exceptions;

public class TransactionProcessingDependencyException : Exception
{
    public TransactionProcessingDependencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
