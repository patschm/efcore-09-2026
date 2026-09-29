namespace WebShop.Reviews.Domain.Identifiers;

// A local reference to a product in the Catalog bounded context - deliberately its own
// type, not shared with Catalog.ProductId (same reasoning as WebShop.Pricing.ProductId).
public readonly record struct ProductId(int Value);
