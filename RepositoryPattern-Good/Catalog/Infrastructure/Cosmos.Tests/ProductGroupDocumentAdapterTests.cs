using Newtonsoft.Json;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Infrastructure.Cosmos.Adapters;

namespace WebShop.Catalog.Infrastructure.Cosmos.Tests;

public class ProductGroupDocumentAdapterTests
{
    [Fact]
    public void Round_trips_including_specification_definitions()
    {
        var group = ProductGroup.Create(new ProductGroupId(44), "Mobiele telefoons", new ProductGroupId(3));
        group.DefineSpecification(
            new SpecificationDefinitionId(2805), "kleur", "Kleur(en)", type: "list", unit: null, multiple: true, explanation: "Beschikbare kleuren");

        var loaded = ProductGroupDocumentAdapter.ToDomain(ProductGroupDocumentAdapter.ToDocument(group));

        Assert.Equal("Mobiele telefoons", loaded.Name);
        Assert.Equal(3, loaded.ParentId?.Value);
        var definition = Assert.Single(loaded.SpecificationDefinitions);
        Assert.Equal(2805, definition.Id.Value);
        Assert.Equal("kleur", definition.Key);
        Assert.Equal("Kleur(en)", definition.Name);
        Assert.Equal("list", definition.Type);
        Assert.True(definition.Multiple);
        Assert.Equal("Beschikbare kleuren", definition.Explanation);
    }

    [Fact]
    public void Root_group_round_trips_with_no_parent()
    {
        var group = ProductGroup.Create(new ProductGroupId(5), "Beeld");

        var loaded = ProductGroupDocumentAdapter.ToDomain(ProductGroupDocumentAdapter.ToDocument(group));

        Assert.Null(loaded.ParentId);
    }

    [Fact]
    public void A_root_groups_document_omits_parentId_entirely_rather_than_serializing_it_as_null()
    {
        // GetByParentId(null) relies on NOT IS_DEFINED(c.parentId) server-side, which only works
        // when the property is genuinely absent from the JSON - not merely present with a null
        // value. NullValueHandling.Ignore on the document is what makes that true; this test
        // exercises the actual serialized shape rather than trusting the attribute is correct.
        var group = ProductGroup.Create(new ProductGroupId(5), "Beeld");

        var json = JsonConvert.SerializeObject(ProductGroupDocumentAdapter.ToDocument(group));

        Assert.DoesNotContain("parentId", json);
    }

    [Fact]
    public void A_child_groups_document_includes_parentId()
    {
        var group = ProductGroup.Create(new ProductGroupId(44), "Mobiele telefoons", new ProductGroupId(3));

        var json = JsonConvert.SerializeObject(ProductGroupDocumentAdapter.ToDocument(group));

        Assert.Contains("\"parentId\":3", json);
    }
}
