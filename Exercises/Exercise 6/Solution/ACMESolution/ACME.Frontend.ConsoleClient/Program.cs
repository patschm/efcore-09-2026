using ACME.DataLayer.Entities;
using ACME.DataLayer.Repository.SqlServer;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ACME.Frontend.ConsoleClient;

internal class Program
{
    const string databaseName = "ProductCatalog";
    const string connectionString = @$"Server=.\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;";
    
    static void Main()
    {
        Exercise1();
        //Exercise2();
        //Exercise3();  
    }

    private static void Exercise1()
    {
        var bld = new DbContextOptionsBuilder<ShopDatabaseContext>();
        bld.UseSqlServer(connectionString);
        var ctx = new ShopDatabaseContext(bld.Options);
        // TODO 1: Use Explicit loading to read related data

        var query = ctx.ProductGroups;
        
        foreach (var item in query)
        {
            Console.WriteLine($"Productgroup: {item.Name}");
            ctx.Entry(item).Collection(p=>p.Products).Load();
            foreach(var product in item.Products)
            {
                ctx.Entry(product).Reference(p => p.Brand).Load();
                Console.WriteLine($"\t* {product.Brand?.Name} {product.Name}");
            }
        }
    }
    private static void Exercise2()
    {
        var bld = new DbContextOptionsBuilder<ShopDatabaseContext>();
        bld.UseSqlServer(connectionString);
        var ctx = new ShopDatabaseContext(bld.Options);
        // TODO 2: Use Eager Loading to read related data
        var query = ctx.ProductGroups
            .Include(p => p.Products)
                .ThenInclude(p => p.Brand);

        foreach (var item in query)
        {
            Console.WriteLine($"Productgroup: {item.Name}");
            foreach (var product in item.Products)
            {
                Console.WriteLine($"\t* {product.Brand?.Name} {product.Name}");
            }
        }
    }
    private static void Exercise3()
    {
        var bld = new DbContextOptionsBuilder<ShopDatabaseContext>();
        bld.UseSqlServer(connectionString);
        bld.UseLazyLoadingProxies();
        var ctx = new ShopDatabaseContext(bld.Options);
        // TODO 3: Use Lazy Loading to read related data
        var query = ctx.ProductGroups;

        foreach (var item in query)
        {
            Console.WriteLine($"Productgroup: {item.Name}");
            foreach (var product in item.Products)
            {
                Console.WriteLine($"\t* {product.Brand?.Name} {product.Name}");
            }
        }
    }
}