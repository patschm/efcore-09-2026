using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Application.Commands;

// Brand.Create only throws (single-field validation) - no cross-field/collection invariant
// here, so there's nothing to wrap in a Result; an invalid Name/Website/Logo simply propagates.
public sealed class CreateBrandCommandHandler(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateBrandCommand>
{
    public async Task Handle(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        var website = command.Website is null ? (Url?)null : new Url(command.Website);
        var logo = command.Logo is null ? (Url?)null : new Url(command.Logo);

        var brand = Brand.Create(command.Id, command.Name, website, logo);

        brandRepository.Add(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
