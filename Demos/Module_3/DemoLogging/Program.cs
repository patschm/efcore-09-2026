using DemoLogging.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;

namespace DemoLogging;

internal class Program
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ProductCatalog2;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=False";

    static void Main(string[] args)
    {
        //Simple();
        //Extensions();
        //Events();
        //DiagnosticListeners();
        //Interceptors();
        InterceptorFun();
    }

    private static void Simple()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        // Add Simple logging
        optionsBuilder.LogTo(m => Console.WriteLine(m), LogLevel.Information);
        //optionsBuilder.LogTo(m => Console.WriteLine(m),
        //    (eventid, loglevel) => loglevel == LogLevel.Information 
        //        || eventid == RelationalEventId.ConnectionOpened);
        // Sensitive data as well
        optionsBuilder.EnableSensitiveDataLogging(true);
        // Detail Information on errors
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.UseLazyLoadingProxies();
        var options = optionsBuilder.Options;
        var context = new ProductContext(options, NullLogger<ProductContext>.Instance);

        foreach(var group in context.ProductGroups)
        {
            Console.WriteLine($"* {group.Name}");
            foreach(var product in group.Products)
            {
                Console.WriteLine($"   -{product.Brand.Name} {product.Name}");
            }
        }
    }
    private static void Extensions()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(svcs =>
            {
                svcs.AddDbContext<ProductContext>(opts =>
                {
                    opts.EnableSensitiveDataLogging();
                    opts.UseSqlServer(connectionString);
                });
            })
            .ConfigureLogging(bld =>
            {
                bld.ClearProviders();
                bld.AddConsole();
            })
            .Build();

        var context = host.Services.GetRequiredService<ProductContext>();

        foreach (var group in context.ProductGroups)
        {
            Console.WriteLine($"* {group.Name}");
        }
    }
    private static void Events()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var options = optionsBuilder.Options;
        var context = new ProductContext(options, NullLogger<ProductContext>.Instance);
        context.ChangeTracker.Tracked += (sender, e)=>
        {
            Console.WriteLine($"Tracking {e.Entry}");
        };

        foreach (var group in context.ProductGroups)
        {
            Console.WriteLine($"* {group.Name}");
        }
    }
    private static void DiagnosticListeners()
    {
        // .NET core wide logging. Works in all .NET applications
        DiagnosticListener.AllListeners.Subscribe(new DiagnosticObserver());

        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var options = optionsBuilder.Options;
        var context = new ProductContext(options, NullLogger<ProductContext>.Instance);
        

        foreach (var group in context.ProductGroups)
        {
            Console.WriteLine($"* {group.Name}");
        }
    }
    private static void Interceptors()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        optionsBuilder.AddInterceptors(new MyCommandInterceptor());
        optionsBuilder.AddInterceptors(new MyConnectionInterceptor());
        optionsBuilder.AddInterceptors(new MyTransactionInterceptor());
        var options = optionsBuilder.Options;
        var context = new ProductContext(options, NullLogger<ProductContext>.Instance);

        using var tran = context.Database.BeginTransaction();
        foreach (var group in context.ProductGroups)
        {
            Console.WriteLine($"[{group.Id}] {group.Name}");
        }
        tran.Rollback();
    }
    private static void InterceptorFun()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        optionsBuilder.AddInterceptors(new SequentialAccesInterceptor());
        var options = optionsBuilder.Options;
        var context = new ProductContext(options, NullLogger<ProductContext>.Instance);

        //context.Database.GetDbConnection().CreateCommand()

        var prod = context.Products
            .TagWith("SequentialAccess")
            .AsNoTracking()
            .Select(x=> new { x.Id, x.Name, x.BrandId, x.Image, x.ProductGroupId })
            .FirstOrDefault();

    }
}