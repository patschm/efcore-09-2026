using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Commands;

public sealed record DeleteReviewCommand(ReviewId Id);
