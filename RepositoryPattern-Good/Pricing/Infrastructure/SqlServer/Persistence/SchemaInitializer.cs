using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Contexts;

namespace WebShop.Pricing.Infrastructure.SqlServer.Persistence;

// See Catalog/Infrastructure/SqlServer/Persistence/SchemaInitializer.cs for why this is
// EnsureCreated rather than migrations or a mapping onto a pre-existing database.
public static class SchemaInitializer
{
    public static async Task EnsureSchemaCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PricingContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
