using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebShop.Web.Identity;

namespace WebShop.Web.Pages.Admin;

[Authorize(Policy = "CanAdministerReviews")]
public sealed class UsersModel(UserManager<ApplicationUser> userManager) : PageModel
{
    public IReadOnlyList<UserRow> Users { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) => Users = await BuildRows(cancellationToken);

    public async Task<IActionResult> OnPostGrantAsync(string userId, string permission)
    {
        await ChangeClaim(userId, permission, grant: true);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(string userId, string permission)
    {
        await ChangeClaim(userId, permission, grant: false);
        return RedirectToPage();
    }

    // Only these two values are ever meaningful as "permission" claims - guards against a
    // tampered form field turning this into a way to write an arbitrary claim onto a user.
    private async Task ChangeClaim(string userId, string permission, bool grant)
    {
        if (permission is not ("reviews:create" or "reviews:administer"))
            return;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return;

        var claims = await userManager.GetClaimsAsync(user);
        var existing = claims.FirstOrDefault(c => c.Type == "permission" && c.Value == permission);

        if (grant && existing is null)
            await userManager.AddClaimAsync(user, new Claim("permission", permission));
        else if (!grant && existing is not null)
            await userManager.RemoveClaimAsync(user, existing);
    }

    private async Task<IReadOnlyList<UserRow>> BuildRows(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.ToListAsync(cancellationToken);
        var rows = new List<UserRow>(users.Count);

        foreach (var user in users)
        {
            var permissions = (await userManager.GetClaimsAsync(user))
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToHashSet();

            rows.Add(new UserRow(
                user.Id, user.Email ?? "", user.DisplayName,
                permissions.Contains("reviews:create"), permissions.Contains("reviews:administer")));
        }

        return rows;
    }

    public sealed record UserRow(string Id, string Email, string DisplayName, bool CanCreate, bool CanAdminister);
}
