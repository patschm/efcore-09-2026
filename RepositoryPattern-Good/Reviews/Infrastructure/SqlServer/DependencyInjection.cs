using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Reviews.Domain.Repositories;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence.Repositories;

namespace WebShop.Reviews.Infrastructure.SqlServer;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsSqlServerInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ReviewsContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IReviewUserRepository, ReviewUserRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IUnitOfWork, ReviewsUnitOfWork>();

        return services;
    }
}
