namespace PseudoMarkets.Security.IdentityServer.Core.Exceptions;

public class IdentityServiceException : Exception
{
    public IdentityServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
