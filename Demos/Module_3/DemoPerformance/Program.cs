using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DemoPerformance;

internal class Program
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ShopDatabase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=False";

    static void Main(string[] args)
    {
        //Diagnostics();
        //ConnectionPooling();
        //CompiledModels();
        //CompiledQueries();
        BenchmarkSwitcher.FromTypes([typeof(BenchMarking)]).RunAll();

        //PerformanceCounters();

    }

    private static void PerformanceCounters()
    {
        // From EF 9.0 on
        // dotnet tool install --global dotnet-counters
        Console.WriteLine("Determine process ID and run:");
        var process = Process.GetCurrentProcess();
        Console.WriteLine($"dotnet counters monitor Microsoft.EntityFrameworkCore -p {process.Id}");
        Console.WriteLine("Then press enter to continue");
        Console.ReadLine();
        CompiledModels();
        Console.ReadLine();
    }

    private static void Diagnostics()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        var options = optionsBuilder.Options;
        var context = new ProductContext(options);

        // 1) Examine timing using Logging. Identify the Linq query by using Tags
        // 2) Copy te query in SSMS and examine the query plan
        // 3) Consider Query Splitting instead of big joins
        // 4) Also check performance counters
        // 5) Use Sql Server Profiles Tool (Tuning Advisor) to get advise for Indices
        var query = context.ProductGroups
            .Include(pg => pg.Products)
                .ThenInclude(p => p.Brand)//.AsSplitQuery()
            .Include(pg => pg.Products)
                .ThenInclude(p => p.Reviews)//.AsSplitQuery()
            .TagWith("Very big one!");
       
        foreach(var group in query)
        {
            Console.WriteLine($"For productgroup {group.Name} we have the following products:");
            foreach(var product in group.Products)
            {
                //Console.WriteLine($"  -{product.Brand.Name} {product.Name} has the following review scores:");
                foreach(var review in product.Reviews)
                {
                    //Console.WriteLine($"       {review.Score}");
                }
            }
        }
    }
    private static void ConnectionPooling()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var options = optionsBuilder.Options;
        var warmup = new ProductContext(options);
        warmup.Reviews.ToList();

        var timers = new Dictionary<string, TimeSpan>() { { "nopool", TimeSpan.Zero }, { "pool", TimeSpan.Zero } };
        for (int j = 0; j< 20; j++)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 100; i++)
            {
                var context = new ProductContext(options);
                context.Reviews.First();
                context.Dispose();
            }
            watch.Stop();
            timers["nopool"] += watch.Elapsed;
            watch.Reset();

            var factory = new PooledDbContextFactory<ProductContext>(options);
            watch.Start();
            for (int i = 0; i < 100; i++)
            {
                using (var ctx = factory.CreateDbContext())
                {
                    ctx.Reviews.First();
                }
            }
            watch.Stop();
            timers["pool"] += watch.Elapsed;
        }
        Console.WriteLine($"Without pooling: It took on average {timers["nopool"]/20} seconds");
        Console.WriteLine($"With pooling: It took on average {timers["pool"] / 20} seconds");
    }
    private static void CompiledModels()
    {
        // First run: dotnet ef dbcontext optimize --output-dir CompiledModels --namespace DemoPerformance
        // Can be found in CLI-tools: dotnet tool install --global dotnet-ef
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        optionsBuilder.UseModel(ProductContextModel.Instance);
        var options = optionsBuilder.Options;

        var optionsBuilder2 = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder2.UseSqlServer(connectionString);
        var options2 = optionsBuilder.Options;

        var timers = new Dictionary<string, TimeSpan>() { { "normal", TimeSpan.Zero }, { "compiled", TimeSpan.Zero } };
        for (int j = 0; j < 20; j++)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 100; i++)
            {
                var context = new ProductContext(options2);
            }
            watch.Stop();
            timers["normal"] += watch.Elapsed;
            watch.Reset();

            watch.Start();
            for (int i = 0; i < 100; i++)
            {
                var context = new ProductContext(options);
            }
            watch.Stop();
            timers["compiled"] += watch.Elapsed;
        }
        Console.WriteLine($"Without compiled models: It took on average {timers["normal"] / 20} seconds");
        Console.WriteLine($"With compiled models: It took on average {timers["compiled"] / 20} seconds");
    }
   
    private static Func<ProductContext, IEnumerable<ProductGroup>> _compiled =
        EF.CompileQuery((ProductContext ctx)=>ctx.ProductGroups
           .Include(pg => pg.Products)
               .ThenInclude(p => p.Brand)
           .Include(pg => pg.Products));
    
    private static void CompiledQueries()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var options = optionsBuilder.Options;
        var context = new ProductContext(options);

        var query = context.ProductGroups
           .Include(pg => pg.Products)
               .ThenInclude(p => p.Brand)
           .Include(pg => pg.Products);

        var timers = new Dictionary<string, TimeSpan>() { { "normal", TimeSpan.Zero }, { "compiled", TimeSpan.Zero } };
        for (int j = 0; j < 20; j++)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 100; i++)
            {
                var dt = query.ToList();        
            }
            watch.Stop();
            timers["normal"] += watch.Elapsed;
            watch.Reset();

            watch.Start();
            for (int i = 0; i < 100; i++)
            {
                var dt = _compiled(context);
            }
            watch.Stop();
            timers["compiled"] += watch.Elapsed;
        }
        Console.WriteLine($"Without compiled models: It took on average {timers["normal"] / 20} seconds");
        Console.WriteLine($"With compiled models: It took on average {timers["compiled"] / 20} seconds");
    }
}