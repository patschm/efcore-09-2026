using System.Text.Json;
using WebShop.Tools.OpsConsole.Models;

namespace WebShop.Tools.OpsConsole.Services;

// The app's login gate. "Interactive browser login" here means clicking Login runs `az login`
// (no --use-device-code), which opens the system's default browser for the actual sign-in - the
// same real browser flow a user would get from a terminal, just triggered from this app's own
// button instead of a command prompt. Deliberately NOT a separate MSAL/Azure.Identity credential:
// every other operation in this app shells out to `az` itself (see ProcessRunner's own comment
// on why), and `az` only ever honors ITS OWN token cache - a token acquired through a different
// library would be invisible to every `az ...` command this app runs afterward. Logging in via
// `az login` keeps exactly one source of truth for "is this app authenticated".
public sealed class AzureAuthService
{
    public async Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default)
    {
        var (result, _, _) = await ProcessRunner.RunAndCaptureAsync(
            "az", ["account", "show", "-o", "json"], cancellationToken: cancellationToken);
        return result.Succeeded;
    }

    public async Task<ProcessResult> LoginAsync(Action<string> onOutputLine, CancellationToken cancellationToken = default) =>
        await ProcessRunner.RunAsync("az", ["login"], onOutputLine, cancellationToken: cancellationToken);

    public async Task<ProcessResult> LogoutAsync(Action<string> onOutputLine, CancellationToken cancellationToken = default) =>
        await ProcessRunner.RunAsync("az", ["logout"], onOutputLine, cancellationToken: cancellationToken);

    // `log` is optional (silent when omitted) since this is also polled quietly at startup, but
    // every caller that can show the user something (MainViewModel) should pass one - a failure
    // here used to be completely invisible (see ProcessRunner's own comment on the stdout/stderr
    // interleaving bug this was hit alongside).
    public async Task<AzureAccountInfo?> GetCurrentAccountAsync(Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        var (result, output, error) = await ProcessRunner.RunAndCaptureAsync(
            "az", ["account", "show", "-o", "json"], cancellationToken: cancellationToken);
        if (!result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(error))
                log?.Invoke(error.Trim());
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;
            return new AzureAccountInfo(
                SubscriptionName: root.GetProperty("name").GetString() ?? "",
                SubscriptionId: root.GetProperty("id").GetString() ?? "",
                UserName: root.GetProperty("user").GetProperty("name").GetString() ?? "");
        }
        catch (JsonException ex)
        {
            log?.Invoke($"Couldn't parse 'az account show' output: {ex.Message}");
            return null;
        }
    }

    public async Task<IReadOnlyList<AzureSubscriptionInfo>> ListSubscriptionsAsync(Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        var (result, output, error) = await ProcessRunner.RunAndCaptureAsync(
            "az", ["account", "list", "-o", "json"], cancellationToken: cancellationToken);
        if (!result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(error))
                log?.Invoke(error.Trim());
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(output);
            return [.. doc.RootElement.EnumerateArray().Select(e => new AzureSubscriptionInfo(
                Name: e.GetProperty("name").GetString() ?? "",
                Id: e.GetProperty("id").GetString() ?? "",
                IsDefault: e.TryGetProperty("isDefault", out var d) && d.GetBoolean()))];
        }
        catch (JsonException ex)
        {
            log?.Invoke($"Couldn't parse 'az account list' output: {ex.Message}");
            return [];
        }
    }

    public async Task<ProcessResult> SetActiveSubscriptionAsync(
        string subscriptionIdOrName, Action<string> onOutputLine, CancellationToken cancellationToken = default) =>
        await ProcessRunner.RunAsync(
            "az", ["account", "set", "--subscription", subscriptionIdOrName], onOutputLine, cancellationToken: cancellationToken);
}
