using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Search.Domain.Repositories;
using WebShop.Search.Infrastructure.Cosmos.Persistence;

namespace WebShop.Search.Infrastructure.Cosmos;

// Mirrors AddSearchPostgresInfrastructure - same shape, new provider, not called from any
// Program.cs (see WebShop.Catalog.Infrastructure.Cosmos.DependencyInjection for why). No
// IUnitOfWork registration here: ProductEmbeddingRepository.Upsert writes immediately, so
// there's nothing for a unit of work to flush.
public static class DependencyInjection
{
    public static IServiceCollection AddSearchCosmosInfrastructure(this IServiceCollection services, CosmosOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton(sp => new CosmosClient(options.ConnectionString));
        services.AddScoped<IProductEmbeddingRepository, ProductEmbeddingRepository>();

        return services;
    }

    public static Task EnsureCosmosContainersCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = services.GetRequiredService<CosmosClient>();
        var options = services.GetRequiredService<CosmosOptions>();
        return CosmosContainerProvisioner.EnsureContainersCreatedAsync(client, options.DatabaseName, cancellationToken);
    }
}
