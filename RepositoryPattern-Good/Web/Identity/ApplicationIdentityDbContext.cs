using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebShop.Web.Identity;

// Lives in its own "identity" schema in the same Postgres database the bounded contexts use -
// this is Web's own infrastructure (login credentials), not a bounded context of its own, so
// it doesn't get a separate Domain/Application/Infrastructure split like Catalog/Pricing/etc.
public sealed class ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("identity");
    }
}
