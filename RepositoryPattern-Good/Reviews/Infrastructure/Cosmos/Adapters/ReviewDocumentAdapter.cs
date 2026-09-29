using System.Globalization;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Infrastructure.Cosmos.Documents;

namespace WebShop.Reviews.Infrastructure.Cosmos.Adapters;

public static class ReviewDocumentAdapter
{
    public static ReviewDocument ToDocument(Review review, ReviewUser? reviewUser) => new()
    {
        Id = ReviewDocument.BuildId(review.Id.Value),
        ProductId = review.ProductId.Value,
        ReviewId = review.Id.Value,
        ReviewType = review.Type,
        Title = review.Title,
        Text = review.Text,
        Score = review.Score,
        ReviewUserId = review.ReviewUserId?.Value,
        Reviewer = reviewUser is null ? null : new ReviewerSnapshot { Name = reviewUser.Name, Email = reviewUser.Email },
        CreationDate = review.CreationDate.ToString("O"),
        IsDeleted = false
    };

    public static Review ToDomain(ReviewDocument document) => Review.Create(
        new ReviewId(document.ReviewId),
        new ProductId(document.ProductId),
        document.ReviewType,
        DateOnly.ParseExact(document.CreationDate, "O", CultureInfo.InvariantCulture),
        document.Title,
        document.Text,
        document.Score,
        document.ReviewUserId.HasValue ? new ReviewUserId(document.ReviewUserId.Value) : null);
}
