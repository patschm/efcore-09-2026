using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Queries;

// Null ParentId means "top-level groups".
public sealed record GetProductGroupsQuery(ProductGroupId? ParentId);
