using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Aggregates;

namespace WebShop.Reviews.Domain.Repositories;

public interface IReviewUserRepository
{
    Task<ReviewUser?> GetById(ReviewUserId id, CancellationToken cancellationToken);

    // For batch-hydrating a list of reviews with their reviewer's name - one round trip
    // regardless of how many distinct reviewers the list references.
    Task<IReadOnlyList<ReviewUser>> GetByIds(IReadOnlyCollection<ReviewUserId> ids, CancellationToken cancellationToken);

    void Add(ReviewUser reviewUser);
}
