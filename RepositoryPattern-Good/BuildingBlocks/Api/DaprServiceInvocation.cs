using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebShop.BuildingBlocks.Api;

// Configures where an inter-service HttpClient should actually point: through the Dapr sidecar's
// service-invocation API when one is present (the AKS cluster's Dapr extension injects a sidecar
// into Pods carrying the dapr.io/enabled annotation - see k8s/10-catalog.yaml and friends), or
// straight at the target's own URL otherwise (docker-compose, `dotnet run` locally - Dapr's
// control plane doesn't exist there today, see project memory on why that's deliberate for now).
//
// DAPR_HTTP_PORT is set automatically by the Dapr sidecar injector into every annotated Pod's
// environment - never by application config - so its mere presence is exactly "am I running next
// to a sidecar", with no extra per-environment flag of our own needed to stay correct.
public static class DaprServiceInvocation
{
    // Deliberately a handler, not "just set BaseAddress to the sidecar's invoke-URL prefix": every
    // *ApiClient in this codebase calls relative paths starting with "/" (e.g. "/product-groups"),
    // and .NET's Uri-combining rules treat a leading "/" as rooted at the host - it silently
    // discards whatever path BaseAddress carried. That's invisible with a path-less direct URL
    // (nothing to discard), but breaks the moment BaseAddress becomes
    // "http://localhost:{port}/v1.0/invoke/{appId}/method/" - every call landed on the sidecar's
    // bare root instead (learned the hard way: shipped once, Web's homepage 500'd against a 404
    // from the sidecar). Rewriting the fully-combined request URI in a handler sidesteps the
    // combining rule entirely and needs zero changes to any *ApiClient's call sites.
    public static IHttpClientBuilder ConfigureServiceInvocation(
        this IHttpClientBuilder builder, IConfiguration configuration, string daprAppId, string directUrlConfigKey)
    {
        var daprHttpPort = configuration["DAPR_HTTP_PORT"];
        if (!string.IsNullOrEmpty(daprHttpPort))
        {
            return builder
                .ConfigureHttpClient(client => client.BaseAddress = new Uri($"http://localhost:{daprHttpPort}/"))
                .AddHttpMessageHandler(() => new DaprInvokeUrlRewriteHandler(daprAppId));
        }

        var directUrl = configuration[directUrlConfigKey]
            ?? throw new InvalidOperationException($"Missing configuration value '{directUrlConfigKey}'.");
        return builder.ConfigureHttpClient(client => client.BaseAddress = new Uri(directUrl));
    }

    private sealed class DaprInvokeUrlRewriteHandler(string appId) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var combined = request.RequestUri!;
            request.RequestUri = new Uri(combined, $"/v1.0/invoke/{appId}/method{combined.PathAndQuery}");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
