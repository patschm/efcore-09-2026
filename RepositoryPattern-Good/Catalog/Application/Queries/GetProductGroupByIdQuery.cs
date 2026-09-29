using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Queries;

public sealed record GetProductGroupByIdQuery(ProductGroupId Id);
