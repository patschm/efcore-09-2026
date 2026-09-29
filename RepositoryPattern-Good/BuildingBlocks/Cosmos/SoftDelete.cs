using Microsoft.Azure.Cosmos;

namespace WebShop.BuildingBlocks.Cosmos;

// Cosmos change feed (classic mode) never surfaces deletes, so a document that must stay
// observable to feed consumers is soft-deleted instead of removed: patch it to isDeleted/
// deletedAt (an ordinary update, visible on the feed), then let Cosmos's per-item TTL purge it
// once consumers have had time to react. Only Review uses this today - it's the only repository
// interface in the domain that exposes a Remove method; every other item type is simply immune
// since the container's default TTL is off unless an item sets its own.
public static class SoftDelete
{
    public static IReadOnlyList<PatchOperation> Patch(DateTime deletedAtUtc, TimeSpan gracePeriod) =>
    [
        PatchOperation.Set("/isDeleted", true),
        PatchOperation.Set("/deletedAt", deletedAtUtc),
        PatchOperation.Set("/ttl", (int)gracePeriod.TotalSeconds)
    ];
}
