using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.ValueObjects;
using WebShop.Catalog.Infrastructure.Cosmos.Adapters;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.Cosmos.Tests;

public class ProductDocumentAdapterTests
{
    private static SpecificationDefinition Definition(ProductGroup group, int id, string key, bool multiple = false)
    {
        var result = group.DefineSpecification(new SpecificationDefinitionId(id), key, key, unit: null, multiple: multiple);
        return result.Value;
    }

    [Fact]
    public void ToDocument_denormalizes_brand_name_and_group_path()
    {
        var product = Product.Create(new ProductId(112), "MPx200", new BrandId(25), new ProductGroupId(44), new Url("https://x/img.png"));
        var groupPath = new[] { new GroupPathEntry { Id = 3, Name = "Telecom" }, new GroupPathEntry { Id = 44, Name = "Mobiele telefoons" } };

        var document = ProductDocumentAdapter.ToDocument(product, "Motorola", groupPath);

        Assert.Equal(112, document.ProductId);
        Assert.Equal("MPx200", document.Name);
        Assert.Equal(25, document.BrandId);
        Assert.Equal("Motorola", document.BrandName);
        Assert.Equal(44, document.ProductGroupId);
        Assert.Equal(2, document.GroupPath.Count);
        Assert.Equal("https://x/img.png", document.ImageUrl);
    }

    [Fact]
    public void ToSpecValueDocuments_keys_multiple_values_for_the_same_definition_by_their_own_id_not_by_definition()
    {
        // The exact shape of the real migration bug: a "multiple"-valued specification (e.g.
        // "Ondersteunde audioformaten": MP3, WMA) has two ProductSpecificationValue rows sharing
        // one SpecificationDefinitionId - keying documents by specDefId would silently collapse
        // them to whichever was written last. Built via Reconstitute (not SetSpecificationValue,
        // which intentionally replaces any existing value for the same definition) since that's
        // the actual path a Cosmos repository's GetById uses to rehydrate a product's values.
        var group = ProductGroup.Create(new ProductGroupId(1), "Telefoons");
        var audioFormats = Definition(group, 10, "audioformaten", multiple: true);
        var product = Product.Reconstitute(
            new ProductId(1), "MPx200", new BrandId(1), new ProductGroupId(1), imageUrl: null,
            specificationValues:
            [
                (new ProductSpecificationValueId(1), audioFormats.Id, SpecificationValue.Create(text: "MP3").Value),
                (new ProductSpecificationValueId(2), audioFormats.Id, SpecificationValue.Create(text: "WMA").Value)
            ]);

        var documents = ProductDocumentAdapter.ToSpecValueDocuments(
            product, group.SpecificationDefinitions.ToDictionary(d => d.Id));

        Assert.Equal(2, documents.Count);
        Assert.Equal(["spec|1", "spec|2"], documents.Select(d => d.Id));
        Assert.Equal(["MP3", "WMA"], documents.Select(d => d.StringValue));
    }

    [Fact]
    public void ToSpecValueDocuments_denormalizes_key_name_and_unit_from_the_definition()
    {
        var group = ProductGroup.Create(new ProductGroupId(1), "Beeld");
        var result = group.DefineSpecification(new SpecificationDefinitionId(5), "screen_size", "Screen size", unit: "inch");
        var product = Product.Create(new ProductId(1), "QLED 55\"", new BrandId(1), new ProductGroupId(1));
        product.SetSpecificationValue(new ProductSpecificationValueId(1), result.Value.Id, SpecificationValue.Create(number: 55m).Value);

        var document = Assert.Single(ProductDocumentAdapter.ToSpecValueDocuments(
            product, group.SpecificationDefinitions.ToDictionary(d => d.Id)));

        Assert.Equal("screen_size", document.Key);
        Assert.Equal("Screen size", document.Name);
        Assert.Equal("inch", document.Unit);
        Assert.Equal(55m, document.NumberValue);
    }

    [Fact]
    public void ToSpecValueDocuments_throws_when_a_value_references_a_definition_outside_its_product_group()
    {
        // This is the exact data-fidelity check the migration tool got wrong originally: it let
        // one bad value abort mapping of the whole product instead of being caught explicitly.
        // Asserting this throws (rather than silently dropping the value) documents that the
        // adapter itself is strict - callers (the migration tool, repositories) are responsible
        // for filtering bad rows before calling this, not this method.
        var group = ProductGroup.Create(new ProductGroupId(1), "Telefoons");
        var product = Product.Create(new ProductId(1), "MPx200", new BrandId(1), new ProductGroupId(1));
        product.SetSpecificationValue(
            new ProductSpecificationValueId(1), new SpecificationDefinitionId(999), SpecificationValue.Create(text: "MP3").Value);

        Assert.Throws<InvalidOperationException>(() => ProductDocumentAdapter.ToSpecValueDocuments(
            product, group.SpecificationDefinitions.ToDictionary(d => d.Id)));
    }

    [Fact]
    public void ToDomain_reassembles_a_product_with_its_sibling_spec_value_documents()
    {
        var productDocument = new ProductDocument
        {
            Id = ProductDocument.BuildId(112),
            ProductId = 112,
            Name = "MPx200",
            BrandId = 25,
            ProductGroupId = 44,
            ImageUrl = null
        };
        var specValueDocuments = new[]
        {
            new SpecValueDocument { Id = "spec|1", ProductId = 112, SpecValueId = 1, SpecDefId = 10, Key = "audioformaten", Name = "Audioformaten", StringValue = "MP3" },
            new SpecValueDocument { Id = "spec|2", ProductId = 112, SpecValueId = 2, SpecDefId = 10, Key = "audioformaten", Name = "Audioformaten", StringValue = "WMA" }
        };

        var product = ProductDocumentAdapter.ToDomain(productDocument, specValueDocuments);

        Assert.Equal(112, product.Id.Value);
        Assert.Equal(2, product.SpecificationValues.Count);
        Assert.Equal(["MP3", "WMA"], product.SpecificationValues.Select(v => v.Value.Text));
    }
}
