using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Reviews.Infrastructure.Postgres.Persistence.Contexts;

public sealed class ReviewsUnitOfWork(ReviewsPgContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
