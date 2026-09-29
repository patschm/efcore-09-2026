using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.ValueObjects;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.Cosmos.Adapters;

public static class ProductDocumentAdapter
{
    public static ProductDocument ToDocument(Product product, string? brandName, IReadOnlyList<GroupPathEntry> groupPath) => new()
    {
        Id = ProductDocument.BuildId(product.Id.Value),
        ProductId = product.Id.Value,
        Name = product.Name,
        BrandId = product.BrandId.Value,
        BrandName = brandName,
        ProductGroupId = product.ProductGroupId?.Value,
        GroupPath = groupPath.ToList(),
        ImageUrl = product.ImageUrl?.Value
    };

    // Definitions is the product's own product group's SpecificationDefinitions, keyed by id -
    // needed to denormalize key/name/unit onto each SpecValueDocument.
    public static IReadOnlyList<SpecValueDocument> ToSpecValueDocuments(
        Product product, IReadOnlyDictionary<SpecificationDefinitionId, SpecificationDefinition> definitionsById)
    {
        var documents = new List<SpecValueDocument>();
        foreach (var value in product.SpecificationValues)
        {
            if (!definitionsById.TryGetValue(value.SpecificationDefinitionId, out var definition))
                throw new InvalidOperationException(
                    $"Product {product.Id.Value} has a specification value for definition {value.SpecificationDefinitionId.Value}, " +
                    "which isn't one of its product group's specification definitions.");

            documents.Add(new SpecValueDocument
            {
                Id = SpecValueDocument.BuildId(value.Id.Value),
                ProductId = product.Id.Value,
                SpecValueId = value.Id.Value,
                SpecDefId = definition.Id.Value,
                Key = definition.Key,
                Name = definition.Name,
                Unit = definition.Unit,
                NumberValue = value.Value.Number,
                StringValue = value.Value.Text,
                BoolValue = value.Value.Flag
            });
        }

        return documents;
    }

    public static Product ToDomain(ProductDocument productDocument, IReadOnlyList<SpecValueDocument> specValueDocuments)
    {
        var specificationValues = specValueDocuments.Select(d =>
        {
            var value = SpecificationValue.Create(d.NumberValue, d.StringValue, d.BoolValue);
            if (value.IsFailure)
                throw new InvalidOperationException($"Corrupt SpecValue document '{d.Id}': {string.Join("; ", value.Errors)}");

            return (new ProductSpecificationValueId(d.SpecValueId), new SpecificationDefinitionId(d.SpecDefId), value.Value);
        });

        return Product.Reconstitute(
            new ProductId(productDocument.ProductId),
            productDocument.Name,
            new BrandId(productDocument.BrandId),
            productDocument.ProductGroupId.HasValue ? new ProductGroupId(productDocument.ProductGroupId.Value) : null,
            Url.TryCreate(productDocument.ImageUrl),
            specificationValues);
    }
}
