namespace WebShop.BuildingBlocks.Application.Abstractions.Persistence;

// One per bounded context, wrapping that context's own DbContext.SaveChangesAsync - commands
// mutate an aggregate via its repository, then call this once to persist the change.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
