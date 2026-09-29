namespace WebShop.Catalog.Contracts;

// Search builds its embedding text from this, not from Catalog's own SpecificationDefinition/
// SpecificationValue types - those never cross the context boundary. Key/Name/DisplayValue are
// plain strings on purpose: Search has no business understanding Catalog's three-shape value union.
public sealed record SpecificationSnapshot(string Key, string Name, string DisplayValue);
