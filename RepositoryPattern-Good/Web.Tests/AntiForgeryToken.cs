using System.Text.RegularExpressions;

namespace WebShop.Web.Tests;

// Razor Pages auto-validates an antiforgery token on every POST - pulls the hidden field's value
// out of a GET response's HTML so a test can round-trip it back on the following POST, the same
// way a real browser form submission does. The matching cookie travels automatically, since
// WebApplicationFactory's client keeps cookies across requests.
internal static partial class AntiForgeryToken
{
    public static async Task<string> ExtractAsync(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        var match = TokenPattern().Match(html);
        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"No antiforgery token found in response for {response.RequestMessage?.RequestUri}.");
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
