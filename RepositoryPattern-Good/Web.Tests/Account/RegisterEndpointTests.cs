using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Web.Identity;

namespace WebShop.Web.Tests.Account;

public sealed class RegisterEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public RegisterEndpointTests(WebApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_creates_the_account_grants_reviews_create_and_signs_in()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));

        HttpRequestMessage? reviewUserRequest = null;
        _factory.ReviewsHandler.OnRequest = request =>
        {
            reviewUserRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.DisplayName"] = "Ada Lovelace",
            ["Input.Email"] = "ada@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.Contains("Set-Cookie"));

        // The account is created before the ReviewUser call - a random id must already exist to
        // send, and it's stamped onto the account only after Reviews confirms the call succeeded.
        Assert.NotNull(reviewUserRequest);
        Assert.Equal("/review-users", reviewUserRequest!.RequestUri!.AbsolutePath);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("ada@example.com");
        Assert.NotNull(user);
        Assert.NotNull(user!.ReviewUserId);

        var claims = await userManager.GetClaimsAsync(user);
        Assert.Contains(claims, c => c.Type == "permission" && c.Value == "reviews:create");
        Assert.DoesNotContain(claims, c => c.Value == "reviews:administer");
    }

    [Fact]
    public async Task Register_with_mismatched_passwords_redisplays_the_form_without_creating_a_user()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));

        var reviewsWasCalled = false;
        _factory.ReviewsHandler.OnRequest = _ =>
        {
            reviewsWasCalled = true;
            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.DisplayName"] = "Grace Hopper",
            ["Input.Email"] = "grace@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "SomethingElse1!",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(reviewsWasCalled);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await userManager.FindByEmailAsync("grace@example.com"));
    }

    [Fact]
    public async Task Register_with_an_email_already_in_use_redisplays_the_form()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _factory.ReviewsHandler.OnRequest = _ => new HttpResponseMessage(HttpStatusCode.OK);

        async Task<HttpResponseMessage> RegisterAsync(string displayName)
        {
            var token = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
            return await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.DisplayName"] = displayName,
                ["Input.Email"] = "duplicate@example.com",
                ["Input.Password"] = "Passw0rd!",
                ["Input.ConfirmPassword"] = "Passw0rd!",
                ["__RequestVerificationToken"] = token
            }));
        }

        var first = await RegisterAsync("First User");
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);

        // Signed in as the first account now - log out so the second attempt isn't just
        // re-registering (which OnPostAsync never even checks for) on top of an active session.
        var logoutToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = logoutToken
        }));

        var second = await RegisterAsync("Second User");

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }
}
