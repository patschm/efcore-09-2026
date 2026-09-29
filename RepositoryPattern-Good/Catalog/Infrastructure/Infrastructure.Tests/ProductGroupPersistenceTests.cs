using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Repositories;

namespace WebShop.Catalog.Infrastructure.Tests;

public class ProductGroupPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<CatalogContext> _options;

    public ProductGroupPersistenceTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<CatalogContext>().UseSqlite(_connection).Options;
        using var setup = new CatalogContext(_options);
        setup.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task ProductGroup_and_its_specification_definitions_round_trip()
    {
        using (var context = new CatalogContext(_options))
        {
            var group = ProductGroup.Create(new ProductGroupId(1), "Televisions");
            group.DefineSpecification(new SpecificationDefinitionId(1), "screen_size", "Screen size", type: "number", unit: "inch");
            new ProductGroupRepository(context).Add(group);
            await context.SaveChangesAsync();
        }

        using var readContext = new CatalogContext(_options);
        var loaded = await new ProductGroupRepository(readContext).GetById(new ProductGroupId(1), default);

        Assert.NotNull(loaded);
        var definition = Assert.Single(loaded.SpecificationDefinitions);
        Assert.Equal("screen_size", definition.Key);
        Assert.Equal("inch", definition.Unit);
    }

    [Fact]
    public async Task Child_group_round_trips_its_parent_id()
    {
        using (var context = new CatalogContext(_options))
        {
            var repository = new ProductGroupRepository(context);
            repository.Add(ProductGroup.Create(new ProductGroupId(1), "Electronics"));
            repository.Add(ProductGroup.Create(new ProductGroupId(2), "Televisions", parentId: new ProductGroupId(1)));
            await context.SaveChangesAsync();
        }

        using var readContext = new CatalogContext(_options);
        var loaded = await new ProductGroupRepository(readContext).GetById(new ProductGroupId(2), default);

        Assert.Equal(new ProductGroupId(1), loaded!.ParentId);
    }
}
