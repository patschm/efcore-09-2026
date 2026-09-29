namespace WebShop.Web.Services.Catalog;

public sealed record ProductSummaryDto(int Id, string Name, int BrandId, string BrandName, string? ImageUrl);
