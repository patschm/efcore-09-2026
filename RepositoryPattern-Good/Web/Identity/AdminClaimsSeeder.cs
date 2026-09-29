using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace WebShop.Web.Identity;

// Ensures every email listed under "Identity:AdminEmails" (config) holds the "reviews:administer"
// claim - lets an admin always be re-established by editing config and restarting, rather than
// needing direct database access (or a chicken-and-egg problem if every admin claim were ever
// removed, including an admin accidentally revoking their own).
public static class AdminClaimsSeeder
{
    public static async Task EnsureAdminClaimsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var adminEmails = configuration.GetSection("Identity:AdminEmails").Get<string[]>() ?? [];
        foreach (var email in adminEmails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                continue;

            var claims = await userManager.GetClaimsAsync(user);
            if (!claims.Any(c => c.Type == "permission" && c.Value == "reviews:administer"))
                await userManager.AddClaimAsync(user, new Claim("permission", "reviews:administer"));
        }
    }
}
