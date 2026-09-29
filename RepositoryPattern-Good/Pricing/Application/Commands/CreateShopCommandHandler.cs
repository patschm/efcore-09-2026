using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Application.Commands;

public sealed class CreateShopCommandHandler(IShopRepository shopRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateShopCommand>
{
    public async Task Handle(CreateShopCommand command, CancellationToken cancellationToken)
    {
        var logo = command.Logo is null ? (Url?)null : new Url(command.Logo);

        var shop = Shop.Create(command.Id, command.Name, new Url(command.Url), logo, command.Rating);

        shopRepository.Add(shop);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
