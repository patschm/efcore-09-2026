namespace WebShop.BuildingBlocks.Application.Abstractions.Messaging;

// Queries read and project straight to a DTO - "not found" is a normal outcome (null/empty),
// not a business-rule violation, so unlike ICommandHandler, TResult here is never Result.
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}
