using System.Text;
using System.Text.Json;

namespace PseudoMarkets.OrderExecution.Tests.Support;

internal static class JwtTestTokenFactory
{
    public static string Create(params string[] roles)
    {
        var header = Base64UrlEncode("""{"alg":"none"}""");
        var payload = Base64UrlEncode(JsonSerializer.Serialize(new
        {
            roles = string.Join(",", roles),
            id = "1000000001"
        }));

        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
