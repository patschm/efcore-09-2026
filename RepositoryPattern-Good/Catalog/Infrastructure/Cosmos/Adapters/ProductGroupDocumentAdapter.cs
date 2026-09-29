using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.Cosmos.Adapters;

public static class ProductGroupDocumentAdapter
{
    public static ProductGroupDocument ToDocument(ProductGroup group) => new()
    {
        Id = ProductGroupDocument.BuildId(group.Id.Value),
        ProductGroupId = group.Id.Value,
        Name = group.Name,
        ImageUrl = group.ImageUrl?.Value,
        ParentId = group.ParentId?.Value,
        SpecificationDefinitions = group.SpecificationDefinitions.Select(d => new SpecificationDefinitionDocument
        {
            SpecificationDefinitionId = d.Id.Value,
            Key = d.Key,
            Name = d.Name,
            Unit = d.Unit,
            ValueType = d.Type,
            Multiple = d.Multiple,
            Explanation = d.Explanation
        }).ToList()
    };

    public static ProductGroup ToDomain(ProductGroupDocument document)
    {
        var group = ProductGroup.Create(
            new ProductGroupId(document.ProductGroupId),
            document.Name,
            document.ParentId.HasValue ? new ProductGroupId(document.ParentId.Value) : null,
            Url.TryCreate(document.ImageUrl));

        foreach (var definition in document.SpecificationDefinitions)
        {
            // Reused as-is, not re-created via a dedicated Reconstitute: DefineSpecification's
            // duplicate-key check only ever sees keys already reconstructed from this same
            // document, so it always succeeds for valid stored data. ProductGroup raises no
            // domain events (unlike Product), so calling it here during rehydration is safe.
            var result = group.DefineSpecification(
                new SpecificationDefinitionId(definition.SpecificationDefinitionId),
                definition.Key,
                definition.Name,
                definition.ValueType,
                definition.Unit,
                definition.Multiple,
                definition.Explanation);

            if (result.IsFailure)
                throw new InvalidOperationException($"Corrupt ProductGroup document '{document.Id}': {string.Join("; ", result.Errors)}");
        }

        return group;
    }
}
