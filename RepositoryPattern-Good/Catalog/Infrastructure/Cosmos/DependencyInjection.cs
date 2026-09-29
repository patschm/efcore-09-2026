using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.Cosmos.Persistence;

namespace WebShop.Catalog.Infrastructure.Cosmos;

// Mirrors AddCatalogPostgresInfrastructure - same shape, new provider.
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogCosmosInfrastructure(this IServiceCollection services, CosmosOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton(sp => new CosmosClient(options.ConnectionString));
        services.AddScoped<CosmosChangeTracker>();

        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductGroupRepository, ProductGroupRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, CosmosUnitOfWork>();
        services.AddScoped<IOutbox, NullOutbox>();

        return services;
    }

    public static Task EnsureCosmosContainersCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = services.GetRequiredService<CosmosClient>();
        var options = services.GetRequiredService<CosmosOptions>();
        return CosmosContainerProvisioner.EnsureContainersCreatedAsync(client, options.DatabaseName, cancellationToken);
    }
}
