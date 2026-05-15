using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Accounts.Interfaces;

public interface IPasswordResetManager
{
    PasswordResetResult ResetPassword(string loginId, string passwordResetKey, string newPassword);
}
