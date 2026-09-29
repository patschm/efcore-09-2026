using Microsoft.AspNetCore.Identity;

namespace WebShop.Web.Identity;

// A login credential, not a business identity - Reviews knows this person only as a ReviewUser
// (name/email, no password), linked back here only by id. Keeping the two separate means
// Reviews never has to know anything about how a reviewer authenticates.
public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public int? ReviewUserId { get; set; }
}
