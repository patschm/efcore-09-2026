using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Reviews.Domain.Repositories;
using WebShop.Reviews.Infrastructure.Postgres.Persistence.Contexts;
using WebShop.Reviews.Infrastructure.Postgres.Persistence.Repositories;

namespace WebShop.Reviews.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsPostgresInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ReviewsPgContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IReviewUserRepository, ReviewUserRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IUnitOfWork, ReviewsUnitOfWork>();

        return services;
    }
}
