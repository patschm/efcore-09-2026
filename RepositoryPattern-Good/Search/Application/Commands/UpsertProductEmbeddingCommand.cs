using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Application.Commands;

// Content is assembled by the caller (e.g. Catalog's product title/specs) - Search never
// references Catalog's own types, only a ProductId, to keep the two contexts decoupled.
public sealed record UpsertProductEmbeddingCommand(ProductId ProductId, string Content);
