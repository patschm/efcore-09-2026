using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DemoNavigation;

internal class Program
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ProductCatalog2;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;";

    static void Main(string[] args)
    {
        //DefaultBehavior();
        //ExplicitLoading();
        EagerLoading();
       // LazyLoading();
       // SplitQueries();
    }

    private static void DefaultBehavior()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new ProductContext(optionsBuilder.Options);
        foreach(var pgroup in context.ProductGroups)
        {
            Console.WriteLine(pgroup.Name);
            foreach(var product in pgroup.Products)
            {
                Console.WriteLine($"\t{product.Brand.Name} {product.Name}");
            }
        }
    }

    private static void ExplicitLoading()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new ProductContext(optionsBuilder.Options);
        foreach (var pgroup in context.ProductGroups)
        {
            Console.WriteLine(pgroup.Name);
            context.Entry(pgroup).Collection(pg => pg.Products).Load();
            foreach (var product in pgroup.Products)
            {
                context.Entry(product).Reference(p => p.Brand).Load();
                Console.WriteLine($"\t{product.Brand.Name} {product.Name}");
            }
        }
    }

    private static void EagerLoading()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        var context = new ProductContext(optionsBuilder.Options);
        var query = context.ProductGroups
            .Include(pg => pg.Products)
                .ThenInclude(p => p.Brand);
           // .Include(pg=>pg.Products).The
                   

        foreach (var pgroup in query)
        {
            pgroup.Name = "Hahah";
            Console.WriteLine(pgroup.Name);
            foreach (var product in pgroup.Products)
            {
                Console.WriteLine($"\t{product.Brand.Name} {product.Name}");
            }
        }


        foreach(var entry in context.ChangeTracker.Entries<Product>())
        {
            Console.WriteLine(entry.State);
        }

    }

    private static void LazyLoading()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        optionsBuilder.UseLazyLoadingProxies();
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        var context = new ProductContext(optionsBuilder.Options);
        
        foreach (var pgroup in context.ProductGroups)
        {
            Console.WriteLine(pgroup.Name);
            foreach (var product in pgroup.Products)
            {
                Console.WriteLine($"\t{product.Brand.Name} {product.Name}");
            }
        }
    }

    private static void SplitQueries()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString, opts => { 
            // opts.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); // Enable globally
        });
        optionsBuilder.LogTo(m => Console.WriteLine(m), Microsoft.Extensions.Logging.LogLevel.Information);

        var context = new ProductContext(optionsBuilder.Options);

        var query = context.ProductGroups
            //.AsSplitQuery()
            .Include(pg => pg.Products)
                .ThenInclude(p => p.Brand)
           .AsSplitQuery();
            

        foreach (var pgroup in query)
        {
            Console.WriteLine(pgroup.Name);
            foreach (var product in pgroup.Products)
            {
                Console.WriteLine($"\t{product.Brand.Name} {product.Name}");
            }
        }
    }
}