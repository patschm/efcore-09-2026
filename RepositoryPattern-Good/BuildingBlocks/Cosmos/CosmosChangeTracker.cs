using Microsoft.Azure.Cosmos;

namespace WebShop.BuildingBlocks.Cosmos;

// Bridges the EF-shaped repository contracts (a synchronous void Add(...)/Remove(...), persisted
// later by a separate IUnitOfWork.SaveChangesAsync) onto the Cosmos SDK, which is async-only.
// Repositories stage writes here instead of calling Cosmos directly from Add/Remove;
// CosmosUnitOfWork.SaveChangesAsync flushes them. Writes that land in the same container+
// partition flush together as one atomic TransactionalBatch; everything else flushes as separate
// calls - Cosmos has no cross-partition transaction to give here even if we wanted one.
//
// StageUpsertBatch exists for a second, related problem: some documents (e.g. a Product, which
// denormalizes its brand's name and category breadcrumb) can't be built without an async lookup,
// which a synchronous Add(...) can't perform either. Its factory runs once, here, during the
// async flush - never inside Add itself.
//
// Known gap versus EF: a GetById in the same unit of work won't see a staged-but-unflushed Add,
// since reads still go straight to Cosmos. Not a problem for how these interfaces are used today
// (command handlers read-then-mutate-then-add, not add-then-read-back), but worth knowing.
public sealed class CosmosChangeTracker
{
    private enum WriteKind { Upsert, Patch }

    private sealed record PendingWrite(
        string ContainerName, PartitionKey PartitionKey, string PartitionKeyGroupKey, string ItemId, WriteKind Kind,
        object? Item, IReadOnlyList<PatchOperation>? PatchOperations);

    private sealed record PendingBatch(
        string ContainerName, PartitionKey PartitionKey, string PartitionKeyGroupKey,
        Func<CancellationToken, Task<IReadOnlyList<(string ItemId, object Item)>>> ItemsFactory);

    private readonly List<PendingWrite> _pending = [];
    private readonly List<PendingBatch> _pendingBatches = [];

    public bool HasPendingWrites => _pending.Count > 0 || _pendingBatches.Count > 0;

    public void StageUpsert(string containerName, PartitionKey partitionKey, string itemId, object item) =>
        _pending.Add(new PendingWrite(containerName, partitionKey, partitionKey.ToString(), itemId, WriteKind.Upsert, item, null));

    public void StagePatch(string containerName, PartitionKey partitionKey, string itemId, IReadOnlyList<PatchOperation> patchOperations) =>
        _pending.Add(new PendingWrite(containerName, partitionKey, partitionKey.ToString(), itemId, WriteKind.Patch, null, patchOperations));

    // One or more documents sharing one partition, built by an async factory resolved at flush
    // time. Use this instead of StageUpsert whenever building the document needs a lookup.
    public void StageUpsertBatch(
        string containerName, PartitionKey partitionKey, Func<CancellationToken, Task<IReadOnlyList<(string ItemId, object Item)>>> itemsFactory) =>
        _pendingBatches.Add(new PendingBatch(containerName, partitionKey, partitionKey.ToString(), itemsFactory));

    public async Task FlushAsync(CosmosClient client, string databaseName, CancellationToken cancellationToken)
    {
        if (_pending.Count == 0 && _pendingBatches.Count == 0)
            return;

        var resolvedWrites = new List<PendingWrite>(_pending);
        foreach (var batch in _pendingBatches)
        {
            var items = await batch.ItemsFactory(cancellationToken);
            resolvedWrites.AddRange(items.Select(i =>
                new PendingWrite(batch.ContainerName, batch.PartitionKey, batch.PartitionKeyGroupKey, i.ItemId, WriteKind.Upsert, i.Item, null)));
        }

        foreach (var group in resolvedWrites.GroupBy(w => (w.ContainerName, w.PartitionKeyGroupKey)))
        {
            var container = client.GetContainer(databaseName, group.Key.ContainerName);
            var writes = group.ToList();
            var partitionKey = writes[0].PartitionKey;

            if (writes.Count == 1)
            {
                await ExecuteSingleAsync(container, writes[0], partitionKey, cancellationToken);
                continue;
            }

            var transactionalBatch = container.CreateTransactionalBatch(partitionKey);
            foreach (var write in writes)
                transactionalBatch = write.Kind == WriteKind.Upsert
                    ? transactionalBatch.UpsertItem(write.Item)
                    : transactionalBatch.PatchItem(write.ItemId, write.PatchOperations!);

            using var response = await transactionalBatch.ExecuteAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new CosmosException(
                    $"Transactional batch write to '{group.Key.ContainerName}' failed with {response.StatusCode}.",
                    response.StatusCode, 0, response.ActivityId, response.RequestCharge);
        }

        _pending.Clear();
        _pendingBatches.Clear();
    }

    private static Task ExecuteSingleAsync(Container container, PendingWrite write, PartitionKey partitionKey, CancellationToken cancellationToken) =>
        write.Kind == WriteKind.Upsert
            ? container.UpsertItemAsync<object>(write.Item!, partitionKey, cancellationToken: cancellationToken)
            : container.PatchItemAsync<object>(write.ItemId, partitionKey, write.PatchOperations!, cancellationToken: cancellationToken);
}
