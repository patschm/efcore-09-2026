using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WebShop.Search.Domain.Aggregates;

namespace WebShop.Search.Infrastructure.SqlServer.Persistence.Contexts;

public class SearchContext : DbContext
{
    public SearchContext(DbContextOptions<SearchContext> options)
        : base(options)
    {
    }

    public DbSet<ProductEmbedding> ProductEmbeddings => Set<ProductEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
