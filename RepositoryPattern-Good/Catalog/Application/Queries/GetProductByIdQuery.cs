using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Queries;

public sealed record GetProductByIdQuery(ProductId Id);
