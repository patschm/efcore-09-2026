using Microsoft.EntityFrameworkCore;

namespace WebShop.Web.Identity;

public static class IdentitySchemaInitializer
{
    // A real migration, not EnsureCreated - EnsureCreated checks whether the physical database
    // already has ANY tables at all (it does: the bounded contexts' own tables live in the same
    // "webshop" database), and silently no-ops if so, regardless of whether the "identity"
    // schema itself exists yet. Migrations track their own history table instead, so they only
    // ever apply what's actually missing.
    public static async Task EnsureIdentitySchemaCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
