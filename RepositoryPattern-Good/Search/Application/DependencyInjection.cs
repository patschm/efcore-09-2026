using Microsoft.Extensions.DependencyInjection;
using WebShop.Search.Application.Commands;
using WebShop.Search.Application.Queries;

namespace WebShop.Search.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSearchApplication(this IServiceCollection services)
    {
        services.AddScoped<UpsertProductEmbeddingCommandHandler>();
        services.AddScoped<SearchProductsQueryHandler>();
        services.AddScoped<FindSimilarProductsQueryHandler>();

        return services;
    }
}
