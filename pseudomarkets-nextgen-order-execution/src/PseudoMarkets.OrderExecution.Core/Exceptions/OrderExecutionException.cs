namespace PseudoMarkets.OrderExecution.Core.Exceptions;

public abstract class OrderExecutionException : Exception
{
    protected OrderExecutionException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
