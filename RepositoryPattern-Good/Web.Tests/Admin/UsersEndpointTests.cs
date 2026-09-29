using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Web.Identity;

namespace WebShop.Web.Tests.Admin;

public sealed class UsersEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public UsersEndpointTests(WebApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_request_is_redirected_to_login()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Admin/Users");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Authenticated_user_without_the_administer_claim_is_redirected_to_login()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _factory.ReviewsHandler.OnRequest = _ => new HttpResponseMessage(HttpStatusCode.OK);

        var registerToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.DisplayName"] = "Regular Shopper",
            ["Input.Email"] = "shopper@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!",
            ["__RequestVerificationToken"] = registerToken
        }));

        var response = await client.GetAsync("/Admin/Users");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Admin_can_view_the_page_and_grant_then_revoke_a_permission()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _factory.ReviewsHandler.OnRequest = _ => new HttpResponseMessage(HttpStatusCode.OK);

        // Registers as a plain user first (the only self-service path), then the administer
        // claim is granted directly - there is no self-service way to become an admin.
        var registerToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.DisplayName"] = "Site Admin",
            ["Input.Email"] = "admin@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!",
            ["__RequestVerificationToken"] = registerToken
        }));

        string otherUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var admin = await userManager.FindByEmailAsync("admin@example.com");
            await userManager.AddClaimAsync(admin!, new Claim("permission", "reviews:administer"));

            var other = new ApplicationUser { UserName = "other@example.com", Email = "other@example.com", DisplayName = "Other User" };
            await userManager.CreateAsync(other, "Passw0rd!");
            otherUserId = other.Id;
        }

        // The cookie issued at registration was minted before the claim above existed - log back
        // in so the fresh cookie actually carries it (matches the page's own documented
        // "immediately the next time they log in" behavior).
        var logoutToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = logoutToken
        }));

        var loginToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Login"));
        await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "admin@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["__RequestVerificationToken"] = loginToken
        }));

        var pageResponse = await client.GetAsync("/Admin/Users");
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        var pageHtml = await pageResponse.Content.ReadAsStringAsync();
        Assert.Contains("other@example.com", pageHtml);

        var grantToken = await AntiForgeryToken.ExtractAsync(pageResponse);
        var grantResponse = await client.PostAsync("/Admin/Users?handler=Grant", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userId"] = otherUserId,
            ["permission"] = "reviews:administer",
            ["__RequestVerificationToken"] = grantToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, grantResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var other = await userManager.FindByIdAsync(otherUserId);
            var claims = await userManager.GetClaimsAsync(other!);
            Assert.Contains(claims, c => c.Type == "permission" && c.Value == "reviews:administer");
        }

        var revokePageResponse = await client.GetAsync("/Admin/Users");
        var revokeToken = await AntiForgeryToken.ExtractAsync(revokePageResponse);
        var revokeResponse = await client.PostAsync("/Admin/Users?handler=Revoke", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userId"] = otherUserId,
            ["permission"] = "reviews:administer",
            ["__RequestVerificationToken"] = revokeToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, revokeResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var other = await userManager.FindByIdAsync(otherUserId);
            var claims = await userManager.GetClaimsAsync(other!);
            Assert.DoesNotContain(claims, c => c.Type == "permission" && c.Value == "reviews:administer");
        }
    }

    [Fact]
    public async Task Granting_a_value_other_than_the_two_known_permissions_is_ignored()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _factory.ReviewsHandler.OnRequest = _ => new HttpResponseMessage(HttpStatusCode.OK);

        var registerToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.DisplayName"] = "Tamper Admin",
            ["Input.Email"] = "tamperadmin@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!",
            ["__RequestVerificationToken"] = registerToken
        }));

        string userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync("tamperadmin@example.com");
            await userManager.AddClaimAsync(user!, new Claim("permission", "reviews:administer"));
            userId = user!.Id;
        }

        var logoutToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = logoutToken
        }));

        var loginToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Login"));
        await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "tamperadmin@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["__RequestVerificationToken"] = loginToken
        }));

        var pageResponse = await client.GetAsync("/Admin/Users");
        var grantToken = await AntiForgeryToken.ExtractAsync(pageResponse);
        await client.PostAsync("/Admin/Users?handler=Grant", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userId"] = userId,
            ["permission"] = "super-admin",
            ["__RequestVerificationToken"] = grantToken
        }));

        using var verifyScope = _factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var reloaded = await verifyUserManager.FindByIdAsync(userId);
        var claims = await verifyUserManager.GetClaimsAsync(reloaded!);
        Assert.DoesNotContain(claims, c => c.Value == "super-admin");
    }
}
