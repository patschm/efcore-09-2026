using Microsoft.Extensions.DependencyInjection;
using WebShop.Reviews.Application.Commands;
using WebShop.Reviews.Application.Queries;

namespace WebShop.Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateReviewUserCommandHandler>();
        services.AddScoped<CreateReviewCommandHandler>();
        services.AddScoped<DeleteReviewCommandHandler>();
        services.AddScoped<UpdateReviewCommandHandler>();
        services.AddScoped<GetReviewByIdQueryHandler>();
        services.AddScoped<GetReviewsByProductIdQueryHandler>();
        services.AddScoped<GetAverageScoresByProductIdsQueryHandler>();

        return services;
    }
}
