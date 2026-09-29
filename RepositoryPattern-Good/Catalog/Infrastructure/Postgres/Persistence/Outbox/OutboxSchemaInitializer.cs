using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Outbox;

public static class OutboxSchemaInitializer
{
    // This context maps onto the real, pre-existing "public" schema tables - EnsureCreated is never
    // appropriate here (the database already exists, so it would silently no-op on all of them
    // anyway). OutboxMessage is the one genuinely new table, in its own "integration" schema so it
    // never looks like part of the legacy model - created idempotently here, once, on startup.
    public static async Task EnsureOutboxSchemaCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogPgContext>();
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE SCHEMA IF NOT EXISTS integration;
            CREATE TABLE IF NOT EXISTS integration."OutboxMessage" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Type" varchar(256) NOT NULL,
                "Content" text NOT NULL,
                "OccurredOnUtc" timestamptz NOT NULL,
                "ProcessedOnUtc" timestamptz NULL
            );
            """, cancellationToken);
    }
}
