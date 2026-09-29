using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Outbox;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Repositories;

namespace WebShop.Catalog.Infrastructure.SqlServer;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogSqlServerInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CatalogContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductGroupRepository, ProductGroupRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, CatalogUnitOfWork>();
        services.AddScoped<IOutbox, EfOutbox>();

        return services;
    }
}
