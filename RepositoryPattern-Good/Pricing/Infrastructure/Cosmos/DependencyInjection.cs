using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Infrastructure.Cosmos.Persistence;

namespace WebShop.Pricing.Infrastructure.Cosmos;

// Mirrors AddPricingPostgresInfrastructure - same shape, new provider, not called from any
// Program.cs (see WebShop.Catalog.Infrastructure.Cosmos.DependencyInjection for why).
public static class DependencyInjection
{
    public static IServiceCollection AddPricingCosmosInfrastructure(this IServiceCollection services, CosmosOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton(sp => new CosmosClient(options.ConnectionString));
        services.AddScoped<CosmosChangeTracker>();

        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IUnitOfWork, CosmosUnitOfWork>();

        return services;
    }

    public static Task EnsureCosmosContainersCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = services.GetRequiredService<CosmosClient>();
        var options = services.GetRequiredService<CosmosOptions>();
        return CosmosContainerProvisioner.EnsureContainersCreatedAsync(client, options.DatabaseName, cancellationToken);
    }
}
