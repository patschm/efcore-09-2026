using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Infrastructure.Postgres.Persistence.Contexts;
using WebShop.Pricing.Infrastructure.Postgres.Persistence.Repositories;

namespace WebShop.Pricing.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPricingPostgresInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PricingPgContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IUnitOfWork, PricingUnitOfWork>();

        return services;
    }
}
