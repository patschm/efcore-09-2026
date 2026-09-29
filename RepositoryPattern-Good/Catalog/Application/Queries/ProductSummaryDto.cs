namespace WebShop.Catalog.Application.Queries;

public sealed record ProductSummaryDto(int Id, string Name, int BrandId, string BrandName, string? ImageUrl);
