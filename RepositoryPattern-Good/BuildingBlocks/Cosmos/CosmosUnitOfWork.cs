using Microsoft.Azure.Cosmos;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.BuildingBlocks.Cosmos;

// One shared implementation for every context - unlike EfOutbox/EfUnitOfWork, which each wrap a
// context-specific DbContext, there's nothing context-specific here: it's just "flush whatever
// this scope staged." Each context's DI extension registers this same class as its IUnitOfWork.
public sealed class CosmosUnitOfWork(CosmosClient client, CosmosOptions options, CosmosChangeTracker changeTracker) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        changeTracker.FlushAsync(client, options.DatabaseName, cancellationToken);
}
