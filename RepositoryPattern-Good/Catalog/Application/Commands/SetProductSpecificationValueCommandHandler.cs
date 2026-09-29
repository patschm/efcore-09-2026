using WebShop.Catalog.Contracts;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Events;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Domain.ValueObjects;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Application.Commands;

// SpecificationValue.Create already returns a Result (the "exactly one of three" invariant) -
// this handler surfaces that as-is, and adds only the one failure mode a domain method can't
// express itself: the aggregate not existing.
public sealed class SetProductSpecificationValueCommandHandler(
    IProductRepository productRepository,
    IProductGroupRepository productGroupRepository,
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    IDomainEventHandler<ProductSpecificationValueSet> specificationValueSetHandler)
    : ICommandHandler<SetProductSpecificationValueCommand, Result>
{
    public async Task<Result> Handle(SetProductSpecificationValueCommand command, CancellationToken cancellationToken)
    {
        var valueResult = SpecificationValue.Create(command.Number, command.Text, command.Flag);
        if (valueResult.IsFailure)
            return Result.Failure(valueResult.Errors);

        var product = await productRepository.GetById(command.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure("Product not found.");

        product.SetSpecificationValue(command.Id, command.SpecificationDefinitionId, valueResult.Value);

        // Enqueued before SaveChangesAsync so the outbox row commits in the same transaction as
        // the specification value change - see IOutbox for why that ordering matters.
        if (product.DomainEvents.OfType<ProductSpecificationValueSet>().Any())
        {
            var snapshot = await BuildSpecificationSnapshot(product, cancellationToken);
            if (snapshot is { } s)
                outbox.Enqueue(new ProductSpecificationsChangedIntegrationEvent(product.Id.Value, s.ProductGroupName, s.Specifications, DateTime.UtcNow));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Filtered by type rather than cast outright - see CreateProductCommandHandler for why.
        foreach (var domainEvent in product.DomainEvents.OfType<ProductSpecificationValueSet>())
            await specificationValueSetHandler.Handle(domainEvent, cancellationToken);

        product.ClearDomainEvents();

        return Result.Success();
    }

    // Search (and any other subscriber) never sees Catalog's own SpecificationDefinition/
    // SpecificationValue types - only plain strings, resolved here from Catalog's own data.
    //
    // Text values are deliberately excluded (along with brand and product name, which never
    // reach this method at all) - free-text specs are inconsistent/noisy, and including them
    // (plus brand/product name) made similarity search cluster on shared brand or naming
    // boilerplate rather than genuine spec similarity, surfacing far too many same-brand
    // "similar" products. Number and flag values are structured and comparable, so they stay.
    // ProductGroupName DOES travel with the snapshot, though - that's category context (e.g.
    // "Televisions"), not brand/product-line naming, so it doesn't cause the same problem.
    private async Task<(string ProductGroupName, IReadOnlyList<SpecificationSnapshot> Specifications)?> BuildSpecificationSnapshot(
        Product product, CancellationToken cancellationToken)
    {
        if (product.ProductGroupId is not { } productGroupId)
            return null;

        var productGroup = await productGroupRepository.GetById(productGroupId, cancellationToken);
        if (productGroup is null)
            return null;

        var specifications = product.SpecificationValues
            .Where(value => value.Value.Text is null)
            .Join(
                productGroup.SpecificationDefinitions,
                value => value.SpecificationDefinitionId,
                definition => definition.Id,
                (value, definition) => new SpecificationSnapshot(definition.Key, definition.Name, Describe(value.Value)))
            .ToList();

        return (productGroup.Name, specifications);
    }

    private static string Describe(SpecificationValue value) =>
        value.Number?.ToString() ?? value.Flag?.ToString() ?? string.Empty;
}
