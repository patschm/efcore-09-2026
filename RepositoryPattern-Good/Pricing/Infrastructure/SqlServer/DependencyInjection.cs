using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Repositories;

namespace WebShop.Pricing.Infrastructure.SqlServer;

public static class DependencyInjection
{
    public static IServiceCollection AddPricingSqlServerInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PricingContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IUnitOfWork, PricingUnitOfWork>();

        return services;
    }
}
