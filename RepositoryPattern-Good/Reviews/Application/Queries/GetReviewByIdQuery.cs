using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Queries;

public sealed record GetReviewByIdQuery(ReviewId Id);
