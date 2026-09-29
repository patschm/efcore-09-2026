using Microsoft.Extensions.DependencyInjection;
using WebShop.Search.Domain.Ports;

namespace WebShop.Search.Infrastructure.Embedding;

public static class DependencyInjection
{
    public static IServiceCollection AddQwen3EmbeddingClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient<IEmbeddingClient, Qwen3EmbeddingClient>(client => client.BaseAddress = baseAddress);
        return services;
    }

    // Overload for callers that need Dapr-vs-direct routing (see
    // DaprServiceInvocation.ConfigureServiceInvocation) rather than a fixed BaseAddress.
    public static IServiceCollection AddQwen3EmbeddingClient(
        this IServiceCollection services, Action<IHttpClientBuilder> configureClient)
    {
        configureClient(services.AddHttpClient<IEmbeddingClient, Qwen3EmbeddingClient>());
        return services;
    }
}
