using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Search.Domain.Repositories;
using WebShop.Search.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Search.Infrastructure.SqlServer.Persistence.Repositories;

namespace WebShop.Search.Infrastructure.SqlServer;

public static class DependencyInjection
{
    public static IServiceCollection AddSearchSqlServerInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SearchContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IProductEmbeddingRepository, ProductEmbeddingRepository>();

        return services;
    }
}
