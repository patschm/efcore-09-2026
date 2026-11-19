using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace DemoLinq;

internal class Program
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ShopDatabase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;";

    static void Main(string[] args)
    {
        //ViaExtensions();
        //ViaIntegrated();
        Dangerous();
    }

    private static void Dangerous()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new ProductContext(optionsBuilder.Options);

        var result = CreateQuery(context);
        var data = result.Where(p => p.Name.StartsWith("A"));

        foreach(var dat in data.Take(10))
            Console.WriteLine(dat.Name);
    }

    private static IQueryable<Product> CreateQuery(ProductContext context)
    {
        return context.Products.OrderBy(p=>p.Name);
    }

    static bool BrandByC(Brand b)
    {
        return b.Name!.StartsWith("C");
    }
    private static void ViaExtensions()
    {
        //bool BrandByC(Brand b)
        //{
        //    return b.Name!.StartsWith("C");
        //}
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new ProductContext(optionsBuilder.Options);

        var all = context.Brands
                        .Where(BrandByC)
                        .Join(context.Products, b => b.Id, p => p.BrandId, (b, p) => new { b, p })
                        .Select(cmp => new { Brand = cmp.b.Name, Product = cmp.p });
        var all2 = all.Skip(0).Take(5);

        foreach(var item in all2)
        {
            Console.WriteLine($"{item.Brand} {item.Product.Name}");         
        }

        // GroupJoin not supported yet :(
        //var gj = context.Brands
        //                .Where(b => b.Name!.StartsWith("C"))
        //                .GroupJoin(context.Products, b => b.Id, p => p.BrandId, (b, p) => new { Brand = b, Products = p });
        //foreach(var item in gj)
        //{
        //    Console.WriteLine($"{item.Brand.Name}");
        //    foreach(var p in item.Products)
        //    {
        //        Console.WriteLine($"\t{p.Name}");
        //    }
        //}
    }

    private static void ViaIntegrated()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        var context = new ProductContext(optionsBuilder.Options);

        var all = from item in context.Brands
                  where item.Name!.StartsWith("C")
                  join item2 in context.Products on item.Id equals item2.BrandId
                  select new { Brand = item.Name, Product = item2 };

        foreach (var item in all)
        {
            Console.WriteLine($"{item.Brand} {item.Product.Name}");
        }
    }
}