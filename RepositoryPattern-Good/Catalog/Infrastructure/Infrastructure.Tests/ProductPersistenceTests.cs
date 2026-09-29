using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.ValueObjects;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Repositories;

namespace WebShop.Catalog.Infrastructure.Tests;

public class ProductPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<CatalogContext> _options;

    public ProductPersistenceTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<CatalogContext>().UseSqlite(_connection).Options;
        using var setup = new CatalogContext(_options);
        setup.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Product_and_its_specification_values_round_trip_through_the_complex_property()
    {
        using (var context = new CatalogContext(_options))
        {
            new BrandRepository(context).Add(Brand.Create(new BrandId(1), "Samsung"));
            var group = ProductGroup.Create(new ProductGroupId(1), "Televisions");
            group.DefineSpecification(new SpecificationDefinitionId(1), "screen_size", "Screen size");
            group.DefineSpecification(new SpecificationDefinitionId(2), "smart_tv", "Smart TV");
            new ProductGroupRepository(context).Add(group);

            var product = Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1), new ProductGroupId(1));
            product.SetSpecificationValue(new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), SpecificationValue.Create(number: 55m).Value);
            product.SetSpecificationValue(new ProductSpecificationValueId(2), new SpecificationDefinitionId(2), SpecificationValue.Create(flag: true).Value);
            new ProductRepository(context).Add(product);
            await context.SaveChangesAsync();
        }

        using var readContext = new CatalogContext(_options);
        var loaded = await new ProductRepository(readContext).GetById(new ProductId(1), default);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.SpecificationValues.Count);
        var numberValue = loaded.SpecificationValues.Single(v => v.SpecificationDefinitionId == new SpecificationDefinitionId(1));
        Assert.Equal(55m, numberValue.Value.Number);
        var flagValue = loaded.SpecificationValues.Single(v => v.SpecificationDefinitionId == new SpecificationDefinitionId(2));
        Assert.True(flagValue.Value.Flag);
    }
}
