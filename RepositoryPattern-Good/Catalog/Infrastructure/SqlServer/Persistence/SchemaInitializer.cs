using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence;

// Unlike Postgres (see Persistence/Outbox/OutboxSchemaInitializer.cs in the Postgres project),
// this provider has no pre-existing legacy database to map onto - there's no real SQL Server
// instance backing this app anywhere, so standing one up means creating its whole schema from
// scratch. EnsureCreated, not migrations: this project has no migration history to apply (until
// this provider switch, SqlServer here was only ever a mapping target for tests, via a real
// SQLite :memory: engine - see Infrastructure.Tests), and a from-scratch demo database has
// nothing to migrate FROM.
public static class SchemaInitializer
{
    public static async Task EnsureSchemaCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
