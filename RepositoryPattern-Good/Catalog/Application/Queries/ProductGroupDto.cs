namespace WebShop.Catalog.Application.Queries;

public sealed record ProductGroupDto(int Id, string Name, int? ParentId, string? ImageUrl);
