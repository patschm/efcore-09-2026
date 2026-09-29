namespace WebShop.Reviews.Application.Queries;

public sealed record ReviewDto(
    int Id,
    int ProductId,
    string Type,
    DateOnly CreationDate,
    string? Title,
    string? Text,
    decimal? Score,
    int? ReviewUserId,
    string? ReviewerName);
