using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WebShop.Reviews.Domain.Aggregates;

namespace WebShop.Reviews.Infrastructure.SqlServer.Persistence.Contexts;

public class ReviewsContext : DbContext
{
    public ReviewsContext(DbContextOptions<ReviewsContext> options)
        : base(options)
    {
    }

    public DbSet<ReviewUser> ReviewUsers => Set<ReviewUser>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
