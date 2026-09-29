using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Contexts;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Outbox;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Repositories;

namespace WebShop.Catalog.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogPostgresInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CatalogPgContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductGroupRepository, ProductGroupRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, CatalogUnitOfWork>();
        services.AddScoped<IOutbox, EfOutbox>();

        return services;
    }
}
