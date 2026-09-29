using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Domain.ValueObjects;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Pricing.Application.Commands;

public sealed class CreatePriceCommandHandler(IPriceRepository priceRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePriceCommand>
{
    public async Task Handle(CreatePriceCommand command, CancellationToken cancellationToken)
    {
        var shopPrice = new Money(command.ShopPriceAmount, command.ShopPriceCurrency);
        var shippingPrice = new Money(command.ShippingPriceAmount, command.ShippingPriceCurrency);

        var price = Price.Create(command.Id, command.ProductId, command.ShopId, shopPrice, shippingPrice, command.InStock);

        priceRepository.Add(price);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
