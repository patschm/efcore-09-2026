using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Domain.Models;

public sealed record SimilarProduct(ProductId ProductId, double Score);
