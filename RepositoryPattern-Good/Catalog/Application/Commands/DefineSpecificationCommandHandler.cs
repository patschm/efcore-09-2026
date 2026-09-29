using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Application.Commands;

// ProductGroup.DefineSpecification already returns a Result (the "no duplicate key" collection
// invariant) - this handler surfaces that as-is, and adds only the one failure mode a domain
// method can't express itself: the aggregate not existing.
public sealed class DefineSpecificationCommandHandler(IProductGroupRepository productGroupRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DefineSpecificationCommand, Result<SpecificationDefinitionId>>
{
    public async Task<Result<SpecificationDefinitionId>> Handle(DefineSpecificationCommand command, CancellationToken cancellationToken)
    {
        var productGroup = await productGroupRepository.GetById(command.ProductGroupId, cancellationToken);
        if (productGroup is null)
            return Result<SpecificationDefinitionId>.Failure("Product group not found.");

        var result = productGroup.DefineSpecification(
            command.Id, command.Key, command.Name, command.Type, command.Unit, command.Multiple, command.Explanation);

        if (result.IsFailure)
            return Result<SpecificationDefinitionId>.Failure(result.Errors);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SpecificationDefinitionId>.Success(result.Value.Id);
    }
}
