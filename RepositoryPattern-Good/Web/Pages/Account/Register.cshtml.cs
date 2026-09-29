using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebShop.Web.Identity;
using WebShop.Web.Services.Reviews;

namespace WebShop.Web.Pages.Account;

public sealed class RegisterModel(
    UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
    ReviewsApiClient reviewsApiClient, JwtTokenService jwtTokenService)
    : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
            return Page();

        var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email, DisplayName = Input.DisplayName };
        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        // Every self-registered account can write reviews immediately, matching today's behavior -
        // "reviews:administer" is deliberately not granted here; it has no self-service path and
        // must be added directly (e.g. via AspNetUserClaims) until an admin UI exists.
        await userManager.AddClaimAsync(user, new Claim("permission", "reviews:create"));

        // Reviews knows this person only as a ReviewUser (name/email), never as a login
        // credential - create the matching one now and link it back onto the Identity account.
        var reviewUserId = Random.Shared.Next(1_000_000_000, int.MaxValue);
        var permissionClaims = await userManager.GetClaimsAsync(user);
        var accessToken = jwtTokenService.CreateAccessToken(user, permissionClaims);
        await reviewsApiClient.CreateReviewUserAsync(reviewUserId, Input.DisplayName, Input.Email, accessToken, HttpContext.RequestAborted);

        user.ReviewUserId = reviewUserId;
        await userManager.UpdateAsync(user);

        await signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(returnUrl ?? "/");
    }

    public sealed class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string DisplayName { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
