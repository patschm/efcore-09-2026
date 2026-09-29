using Microsoft.EntityFrameworkCore;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Repositories;
using WebShop.Reviews.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Reviews.Infrastructure.Postgres.Persistence.Repositories;

public sealed class ReviewUserRepository(ReviewsPgContext context) : IReviewUserRepository
{
    public Task<ReviewUser?> GetById(ReviewUserId id, CancellationToken cancellationToken) =>
        context.ReviewUsers.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReviewUser>> GetByIds(IReadOnlyCollection<ReviewUserId> ids, CancellationToken cancellationToken) =>
        await context.ReviewUsers.Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);

    public void Add(ReviewUser reviewUser) => context.ReviewUsers.Add(reviewUser);
}
