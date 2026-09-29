using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebShop.Search.Infrastructure.SqlServer.Persistence.Contexts;

namespace WebShop.Search.Infrastructure.Tests;

// ProductEmbeddingRepository's actual read/write/similarity logic is raw, provider-specific SQL
// (pgvector's <=> and ON CONFLICT for Postgres; VECTOR_DISTANCE and MERGE for SQL Server) that
// SQLite can't execute at all - so unlike the other contexts, there's no meaningful way to
// exercise it here. This just confirms the EF model itself (keys, conversions, max lengths)
// is valid and can create its table. Real coverage of the vector read/write/similarity path
// lives in the manual EfSearchPgSmoke check against a real Postgres server.
public class ProductEmbeddingModelTests
{
    [Fact]
    public void Model_creates_its_table_without_error()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SearchContext>().UseSqlite(connection).Options;

        using var context = new SearchContext(options);

        Assert.True(context.Database.EnsureCreated());
    }
}
