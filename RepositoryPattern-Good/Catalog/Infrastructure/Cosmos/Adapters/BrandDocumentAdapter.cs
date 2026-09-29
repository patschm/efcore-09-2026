using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.Cosmos.Adapters;

public static class BrandDocumentAdapter
{
    public static BrandDocument ToDocument(Brand brand) => new()
    {
        Id = BrandDocument.BuildId(brand.Id.Value),
        BrandId = brand.Id.Value,
        Name = brand.Name,
        Website = brand.Website?.Value,
        Logo = brand.Logo?.Value
    };

    public static Brand ToDomain(BrandDocument document) => Brand.Create(
        new BrandId(document.BrandId),
        document.Name,
        Url.TryCreate(document.Website),
        Url.TryCreate(document.Logo));
}
