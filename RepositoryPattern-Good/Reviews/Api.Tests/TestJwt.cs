using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WebShop.Reviews.Api.Tests;

// Mints tokens the same way WebShop.Web's JwtTokenService does, signed with the same dev-only
// key baked into Reviews/Api/appsettings.json - stands in for the real Web BFF in these tests.
internal static class TestJwt
{
    private const string SigningKey = "Kx7mQ2vL9pR4tN8wZ1yB6cF3hJ5sV0uE9oI2aD7gM4kP1rT6xC8lW3nA5qY0bHs";
    private const string Issuer = "WebShop.Web";
    private const string Audience = "WebShop.Reviews";

    // Defaults to "reviews:create" so existing tests that don't care about authorization keep
    // exercising the happy path - pass permissions explicitly to test a specific grant (or lack
    // of one).
    public static string CreateToken(int? reviewUserId = null, IEnumerable<string>? permissions = null)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, "test-user") };
        if (reviewUserId is { } id)
            claims.Add(new Claim("reviewUserId", id.ToString()));

        foreach (var permission in permissions ?? ["reviews:create"])
            claims.Add(new Claim("permission", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
