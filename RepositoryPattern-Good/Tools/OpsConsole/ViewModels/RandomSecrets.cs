using System.Security.Cryptography;

namespace WebShop.Tools.OpsConsole.ViewModels;

// Same generation approach used by hand this session (openssl rand -base64) - .NET's own RNG
// instead of shelling out to openssl, since there's no other reason to depend on it here.
public static class RandomSecrets
{
    public static string GenerateJwtSigningKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    // Postgres/Cosmos admin passwords: base64 can contain characters some tools mishandle
    // unquoted (/, +, =) - trimmed to a plain alphanumeric string instead, with a fixed suffix
    // guaranteeing at least one uppercase/lowercase/digit to satisfy Azure's complexity rules.
    public static string GenerateAdminPassword()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))
            .Replace("/", "").Replace("+", "").Replace("=", "");
        return raw[..Math.Min(raw.Length, 20)] + "Aa1";
    }
}
