using System.Security.Cryptography;
using System.Text;

namespace PseudoMarkets.Security.IdentityServer.Core.Accounts;

public static class PasswordResetKeyProtector
{
    public static string GenerateKey()
    {
        return Guid.NewGuid().ToString("D");
    }

    public static string HashKey(string passwordResetKey)
    {
        if (!TryNormalize(passwordResetKey, out var normalizedKey))
        {
            return string.Empty;
        }

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedKey)));
    }

    public static bool VerifyHash(string storedHash, string providedKey)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || !TryNormalize(providedKey, out var normalizedKey))
        {
            return false;
        }

        try
        {
            var expectedHash = Convert.FromBase64String(storedHash);
            var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedKey));
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryNormalize(string? passwordResetKey, out string normalizedKey)
    {
        normalizedKey = string.Empty;
        if (!Guid.TryParse(passwordResetKey, out var parsedGuid))
        {
            return false;
        }

        normalizedKey = parsedGuid.ToString("D");
        return true;
    }
}
