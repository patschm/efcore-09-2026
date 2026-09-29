using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Reviews.Infrastructure.SqlServer.Persistence.Contexts;

public sealed class ReviewsUnitOfWork(ReviewsContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
