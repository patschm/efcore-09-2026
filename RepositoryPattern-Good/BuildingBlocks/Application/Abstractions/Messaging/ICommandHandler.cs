namespace WebShop.BuildingBlocks.Application.Abstractions.Messaging;

// Plain handler contracts, not a mediator: callers depend on the specific
// ICommandHandler<TCommand> they need and call Handle directly - no generic Send(command)
// dispatch, no runtime handler lookup, no pipeline behaviors.
//
// Result only appears as TResult where the command's own domain operation genuinely produces
// one (a cross-field/collection invariant, same line the domain itself already draws) -
// commands backed only by throwing single-field validation return void/a plain value instead,
// letting that exception propagate rather than wrapping it artificially at this boundary.
public interface ICommandHandler<in TCommand>
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}
