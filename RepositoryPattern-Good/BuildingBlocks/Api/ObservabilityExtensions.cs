using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WebShop.BuildingBlocks.Api;

// One shared setup for tracing, metrics, and logging across every service - where it actually
// gets exported is controlled entirely by the standard OTEL_EXPORTER_OTLP_* environment variables
// (see UseOtlpExporter below), so nothing here is tied to a specific backend: an OpenTelemetry
// Collector, Jaeger, the .NET Aspire dashboard, Azure Monitor via a collector, all work the same
// way. With no endpoint configured, the SDK defaults to http://localhost:4317 per the OpenTelemetry
// spec and just logs failed export attempts periodically rather than crashing the app - that's
// expected when running locally without a collector.
public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddWebShopObservability(this IHostApplicationBuilder builder, string serviceName)
    {
        // Every *.Api.Tests/Web.Tests suite spins up a full WebApplicationFactory host per test -
        // with no collector reachable in CI/local test runs, the OTLP exporter's failed-connection
        // retries on every host startup/shutdown turned what was a few seconds per test project
        // into tens of seconds. Skipping this under "Testing" (same convention already used to
        // skip Postgres bootstrapping there) keeps tests fast without touching any test code.
        if (builder.Environment.IsEnvironment("Testing"))
            return builder;

        builder.Logging.AddOpenTelemetry(logging =>
        {
            // Attaches the active trace/span id to every log entry - what actually makes
            // "tracing and logging together" useful (jump from a span straight to its logs).
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // Covers every context here - all four bounded-context APIs and Web's own
                // Identity store go through EF Core, whether the Postgres or SqlServer flavor
                // is wired up. This beta package has no "include SQL text" switch of its own
                // (older versions did); EnrichWithIDbCommand is the current way to add it.
                .AddEntityFrameworkCoreInstrumentation(options =>
                    options.EnrichWithIDbCommand = (activity, command) => activity.SetTag("db.statement", command.CommandText)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            // Reads OTEL_EXPORTER_OTLP_ENDPOINT / _PROTOCOL / _HEADERS and applies an OTLP
            // exporter to whichever signals are registered above (traces, metrics, and - since
            // logging.AddOpenTelemetry was called too - logs), instead of configuring each
            // exporter by hand.
            .UseOtlpExporter();

        return builder;
    }
}
