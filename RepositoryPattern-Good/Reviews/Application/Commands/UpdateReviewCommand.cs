using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Commands;

public sealed record UpdateReviewCommand(ReviewId Id, string? Title, string? Text, decimal? Score);
