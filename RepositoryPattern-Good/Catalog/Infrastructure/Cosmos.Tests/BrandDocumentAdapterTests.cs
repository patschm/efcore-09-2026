using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Infrastructure.Cosmos.Adapters;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.Cosmos.Tests;

public class BrandDocumentAdapterTests
{
    [Fact]
    public void Round_trips_including_website_and_logo()
    {
        var brand = Brand.Create(
            new BrandId(25), "Motorola", new Url("https://www.motorola.com"), new Url("https://www.motorola.com/logo.png"));

        var document = BrandDocumentAdapter.ToDocument(brand);
        var loaded = BrandDocumentAdapter.ToDomain(document);

        Assert.Equal("25", document.Id);
        Assert.Equal(25, document.BrandId);
        Assert.Equal("Motorola", loaded.Name);
        Assert.Equal(25, loaded.Id.Value);
        Assert.Equal("https://www.motorola.com", loaded.Website?.Value);
        Assert.Equal("https://www.motorola.com/logo.png", loaded.Logo?.Value);
    }

    [Fact]
    public void Round_trips_without_website_or_logo()
    {
        var brand = Brand.Create(new BrandId(1), "Generic");

        var loaded = BrandDocumentAdapter.ToDomain(BrandDocumentAdapter.ToDocument(brand));

        Assert.Null(loaded.Website);
        Assert.Null(loaded.Logo);
    }

    [Fact]
    public void BuildId_is_the_bare_brand_id()
    {
        Assert.Equal("25", BrandDocument.BuildId(25));
    }
}
