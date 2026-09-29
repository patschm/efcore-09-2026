using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Repositories;

namespace WebShop.Reviews.Application.Tests.Fakes;

internal sealed class FakeReviewUserRepository : IReviewUserRepository
{
    private readonly Dictionary<ReviewUserId, ReviewUser> _reviewUsers = [];

    public Task<ReviewUser?> GetById(ReviewUserId id, CancellationToken cancellationToken) =>
        Task.FromResult(_reviewUsers.GetValueOrDefault(id));

    public Task<IReadOnlyList<ReviewUser>> GetByIds(IReadOnlyCollection<ReviewUserId> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReviewUser>>(_reviewUsers.Values.Where(u => ids.Contains(u.Id)).ToList());

    public void Add(ReviewUser reviewUser) => _reviewUsers[reviewUser.Id] = reviewUser;
}
