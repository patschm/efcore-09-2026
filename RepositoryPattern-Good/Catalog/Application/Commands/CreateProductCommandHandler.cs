using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Events;
using WebShop.Catalog.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Application.Commands;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IDomainEventHandler<ProductCreated> productCreatedHandler) : ICommandHandler<CreateProductCommand>
{
    public async Task Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var imageUrl = command.ImageUrl is null ? (Url?)null : new Url(command.ImageUrl);

        var product = Product.Create(command.Id, command.Name, command.BrandId, command.ProductGroupId, imageUrl);

        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatched explicitly, once persistence has succeeded - no generic dispatcher, same
        // discipline as ICommandHandler/IQueryHandler. Filtered by type rather than cast
        // outright: DomainEvents could in principle hold other event types too (e.g. if the
        // same tracked instance is reused across commands within one unit-of-work scope).
        foreach (var domainEvent in product.DomainEvents.OfType<ProductCreated>())
            await productCreatedHandler.Handle(domainEvent, cancellationToken);

        product.ClearDomainEvents();
    }
}
