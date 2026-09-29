using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Search.Domain.Repositories;
using WebShop.Search.Infrastructure.Postgres.Persistence.Contexts;
using WebShop.Search.Infrastructure.Postgres.Persistence.Repositories;

namespace WebShop.Search.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddSearchPostgresInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SearchPgContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IProductEmbeddingRepository, ProductEmbeddingRepository>();

        return services;
    }
}
