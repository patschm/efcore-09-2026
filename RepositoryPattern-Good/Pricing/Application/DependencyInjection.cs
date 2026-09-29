using Microsoft.Extensions.DependencyInjection;
using WebShop.Pricing.Application.Commands;
using WebShop.Pricing.Application.Queries;

namespace WebShop.Pricing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPricingApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateShopCommandHandler>();
        services.AddScoped<CreatePriceCommandHandler>();
        services.AddScoped<GetPriceByIdQueryHandler>();
        services.AddScoped<GetPricesByProductIdQueryHandler>();
        services.AddScoped<GetLowestPricesByProductIdsQueryHandler>();

        return services;
    }
}
