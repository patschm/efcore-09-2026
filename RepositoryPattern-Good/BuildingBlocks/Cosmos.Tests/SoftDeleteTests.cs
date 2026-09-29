using Microsoft.Azure.Cosmos;

namespace WebShop.BuildingBlocks.Cosmos.Tests;

public class SoftDeleteTests
{
    [Fact]
    public void Patch_sets_isDeleted_deletedAt_and_ttl()
    {
        var operations = SoftDelete.Patch(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), TimeSpan.FromDays(7));

        Assert.Collection(operations,
            op => { Assert.Equal(PatchOperationType.Set, op.OperationType); Assert.Equal("/isDeleted", op.Path); },
            op => { Assert.Equal(PatchOperationType.Set, op.OperationType); Assert.Equal("/deletedAt", op.Path); },
            op => { Assert.Equal(PatchOperationType.Set, op.OperationType); Assert.Equal("/ttl", op.Path); });
    }
}
