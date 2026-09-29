using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Application.Commands;

public sealed class CreateProductGroupCommandHandler(IProductGroupRepository productGroupRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductGroupCommand>
{
    public async Task Handle(CreateProductGroupCommand command, CancellationToken cancellationToken)
    {
        var imageUrl = command.ImageUrl is null ? (Url?)null : new Url(command.ImageUrl);

        var productGroup = ProductGroup.Create(command.Id, command.Name, command.ParentId, imageUrl);

        productGroupRepository.Add(productGroup);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
