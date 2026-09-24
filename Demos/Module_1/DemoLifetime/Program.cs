using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DemoLifetime;

internal class Program
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ProductCatalog;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;";

    static void Main(string[] args)
    {
        //DemoOptions();
        //DemoDirectInit();
        //DemoDI_Init();
        //DemoFactoryInit();
        DemoPooling();
    }

    
    private static void DemoOptions()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString, opts =>{
            opts.MinBatchSize(16);
            opts.CommandTimeout(300);
            opts.EnableRetryOnFailure(2, TimeSpan.FromSeconds(5), null);
        });
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.LogTo(s => Console.WriteLine(s));

        string brnd = "M";
        using (var context = new ProductContext(optionsBuilder.Options))
        {
            foreach (var brand in context.Brands)
            {
                Console.WriteLine(brand.Name);
            }
        }
    }

    private static void DemoDirectInit()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseSqlServer(connectionString);
        using (var context = new ProductContext(optionsBuilder.Options))
        {
            foreach (var brand in context.Brands)
            {
                Console.WriteLine(brand.Name);
            }
        }
    }
    private static void DemoDI_Init()
    {
        Host.CreateDefaultBuilder()
            .ConfigureServices(svcs => {
                svcs.AddHostedService<DIApp>();
                svcs.AddDbContext<ProductContext>(opts => {
                    opts.UseSqlServer(connectionString);
                });
            })
            .Build()
            .Run();
    }
    private static void DemoFactoryInit()
    {
        Host.CreateDefaultBuilder()
           .ConfigureServices(svcs => {
               svcs.AddHostedService<DIFactoryApp>();
               svcs.AddDbContextFactory<ProductContext>(opts => {
                   opts.UseSqlServer(connectionString);
               });
           })
           .Build()
           .Run();
    }
    private static void DemoPooling()
    {
        // Direct
        //var options = new DbContextOptionsBuilder<ProductContext>()
        //    .UseSqlServer(connectionString).Options;
        //var factory = new PooledDbContextFactory<ProductContext>(options);
        //using (var ctx = factory.CreateDbContext())
        //{

        //}

        Host.CreateDefaultBuilder()
          .ConfigureServices(svcs => {
              svcs.AddHostedService<DIPoolingApp>();
              svcs.AddDbContextPool<ProductContext>(opts => {
                  opts.UseSqlServer(connectionString);
              });
          })
          .Build()
          .Run();
    }

}