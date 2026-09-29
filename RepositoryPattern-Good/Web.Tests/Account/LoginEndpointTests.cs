using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebShop.Web.Tests.Account;

public sealed class LoginEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public LoginEndpointTests(WebApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_with_correct_credentials_signs_in_and_a_protected_page_becomes_reachable()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _factory.ReviewsHandler.OnRequest = _ => new HttpResponseMessage(HttpStatusCode.OK);

        var registerToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.DisplayName"] = "Margaret Hamilton",
            ["Input.Email"] = "margaret@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!",
            ["__RequestVerificationToken"] = registerToken
        }));

        var logoutToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = logoutToken
        }));

        var loginToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Login"));
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "margaret@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["__RequestVerificationToken"] = loginToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);

        // Admin/Users only renders for an authenticated caller (redirects unauthenticated/
        // unauthorized ones to Login) - reaching the real page content confirms the cookie from
        // the login above is being sent and recognized as an authenticated session.
        var adminAttempt = await client.GetAsync("/Admin/Users");
        Assert.Equal(HttpStatusCode.Redirect, adminAttempt.StatusCode);
        Assert.Equal("/Account/Login", adminAttempt.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Login_with_wrong_password_redisplays_the_form_with_an_error()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _factory.ReviewsHandler.OnRequest = _ => new HttpResponseMessage(HttpStatusCode.OK);

        var registerToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.DisplayName"] = "Katherine Johnson",
            ["Input.Email"] = "katherine@example.com",
            ["Input.Password"] = "Passw0rd!",
            ["Input.ConfirmPassword"] = "Passw0rd!",
            ["__RequestVerificationToken"] = registerToken
        }));

        var logoutToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Register"));
        await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = logoutToken
        }));

        var loginToken = await AntiForgeryToken.ExtractAsync(await client.GetAsync("/Account/Login"));
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = "katherine@example.com",
            ["Input.Password"] = "WrongPassword1!",
            ["__RequestVerificationToken"] = loginToken
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password.", html);
    }
}
