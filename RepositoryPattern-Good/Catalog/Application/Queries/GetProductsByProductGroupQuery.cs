using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Queries;

public sealed record GetProductsByProductGroupQuery(ProductGroupId ProductGroupId, int Page = 1, int PageSize = 20);
