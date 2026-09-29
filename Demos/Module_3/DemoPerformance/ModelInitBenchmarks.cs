using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.EntityFrameworkCore;

namespace DemoPerformance;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 10, warmupCount: 0, iterationCount: 1)]
public class ModelInitBenchmarks
{
    public static string connectionString = BenchMarking.connectionString;

    [Benchmark(Baseline = true)]
    public int NormalInit()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var options = optionsBuilder.Options;
        using var context = new ProductContext(options);
        return context.Model.GetEntityTypes().Count(); // forces the lazy model build
    }

    [Benchmark]
    public int CompiledModelInit()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        optionsBuilder.UseModel(ProductContextModel.Instance);
        var options = optionsBuilder.Options;
        using var context = new ProductContext(options);
        return context.Model.GetEntityTypes().Count(); // forces the lazy model build
    }
}
