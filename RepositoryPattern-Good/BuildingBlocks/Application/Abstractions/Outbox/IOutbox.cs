using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.BuildingBlocks.Application.Abstractions.Outbox;

// Enqueue only adds to the owning DbContext's change tracker - it does not save. Callers must
// enqueue before their own IUnitOfWork.SaveChangesAsync so the outbox row commits in the same
// transaction as the aggregate change it describes. That's what makes the write atomic without
// a distributed transaction: either both land, or neither does.
public interface IOutbox
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
