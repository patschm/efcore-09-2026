using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Commands;

public sealed record CreateReviewUserCommand(ReviewUserId Id, string Name, string? Email = null);
