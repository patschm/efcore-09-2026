using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Application.Events;
using WebShop.Catalog.Application.Queries;
using WebShop.Catalog.Domain.Events;

namespace WebShop.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        // Stand-ins - these only log, they don't yet do anything a real subscriber would care about
        // in-process. See BuildingBlocks.Application.Abstractions.Outbox for the actual cross-context
        // mechanism (ProductSpecificationValueSet's cross-context effect goes through the outbox
        // instead, in SetProductSpecificationValueCommandHandler).
        services.AddScoped<IDomainEventHandler<ProductCreated>, LogProductCreatedHandler>();
        services.AddScoped<IDomainEventHandler<ProductSpecificationValueSet>, LogProductSpecificationValueSetHandler>();

        services.AddScoped<CreateBrandCommandHandler>();
        services.AddScoped<CreateProductGroupCommandHandler>();
        services.AddScoped<DefineSpecificationCommandHandler>();
        services.AddScoped<CreateProductCommandHandler>();
        services.AddScoped<SetProductSpecificationValueCommandHandler>();
        services.AddScoped<GetProductByIdQueryHandler>();
        services.AddScoped<GetProductGroupsQueryHandler>();
        services.AddScoped<GetProductGroupByIdQueryHandler>();
        services.AddScoped<GetProductsByProductGroupQueryHandler>();
        services.AddScoped<GetProductsByIdsQueryHandler>();

        return services;
    }
}
