using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WebShop.Web.Identity;

// Mints a short-lived token proving "this call comes from Web, on behalf of this
// authenticated user" - handed to Reviews.Api as a Bearer token so it can trust the caller's
// identity (in particular, ReviewUserId) without Web and Reviews needing a shared user store.
// No separate auth server: this is only ever validated by services that share this signing key.
public sealed class JwtTokenService(IConfiguration configuration)
{
    // permissionClaims come from the caller's own AspNetUserClaims (via UserManager.GetClaimsAsync)
    // rather than being looked up in here, so this service stays a pure token-shaping step with
    // no dependency on UserManager or how permissions are stored.
    public string CreateAccessToken(ApplicationUser user, IEnumerable<Claim> permissionClaims)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new("name", user.DisplayName)
        };

        if (user.ReviewUserId is { } reviewUserId)
            claims.Add(new Claim("reviewUserId", reviewUserId.ToString()));

        claims.AddRange(permissionClaims.Where(c => c.Type == "permission"));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
