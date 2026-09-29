using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Infrastructure.Cosmos.Documents;

namespace WebShop.Reviews.Infrastructure.Cosmos.Adapters;

public static class ReviewUserDocumentAdapter
{
    public static ReviewUserDocument ToDocument(ReviewUser reviewUser) => new()
    {
        Id = ReviewUserDocument.BuildId(reviewUser.Id.Value),
        ReviewUserId = reviewUser.Id.Value,
        Name = reviewUser.Name,
        Email = reviewUser.Email
    };

    public static ReviewUser ToDomain(ReviewUserDocument document) =>
        ReviewUser.Create(new ReviewUserId(document.ReviewUserId), document.Name, document.Email);
}
