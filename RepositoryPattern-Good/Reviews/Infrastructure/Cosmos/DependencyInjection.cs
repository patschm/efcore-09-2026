using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Reviews.Domain.Repositories;
using WebShop.Reviews.Infrastructure.Cosmos.Persistence;

namespace WebShop.Reviews.Infrastructure.Cosmos;

// Mirrors AddReviewsPostgresInfrastructure - same shape, new provider, not called from any
// Program.cs (see WebShop.Catalog.Infrastructure.Cosmos.DependencyInjection for why).
public static class DependencyInjection
{
    public static IServiceCollection AddReviewsCosmosInfrastructure(this IServiceCollection services, CosmosOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton(sp => new CosmosClient(options.ConnectionString));
        services.AddScoped<CosmosChangeTracker>();

        services.AddScoped<IReviewUserRepository, ReviewUserRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
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
