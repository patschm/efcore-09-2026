namespace WebShop.Web.Services.Catalog;

public sealed record ProductGroupDto(int Id, string Name, int? ParentId, string? ImageUrl);
