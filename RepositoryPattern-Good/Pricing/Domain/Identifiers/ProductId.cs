namespace WebShop.Pricing.Domain.Identifiers;

// A local reference to a product in the Catalog bounded context - deliberately its own
// type, not shared with Catalog.ProductId, so WebShop.Pricing never needs a project
// reference to WebShop.Catalog just to name an id.
public readonly record struct ProductId(int Value);
