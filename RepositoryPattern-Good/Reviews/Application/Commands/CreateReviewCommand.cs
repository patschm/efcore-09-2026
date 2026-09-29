using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Commands;

public sealed record CreateReviewCommand(
    ReviewId Id,
    ProductId ProductId,
    string Type,
    DateOnly CreationDate,
    string? Title = null,
    string? Text = null,
    decimal? Score = null,
    ReviewUserId? ReviewUserId = null);
