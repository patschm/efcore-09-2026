using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoPerformance;

[MemoryDiagnoser]
[MaxIterationCount(200)]
public class BenchMarking
{
    public static string connectionString = @"Server=.\SQLEXPRESS;Database=ShopDatabase;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Encrypt=False";

    // NormalInit/NormalCompiledModelInit moved to ModelInitBenchmarks.cs - comparing DbContext
    // construction cost needs a cold-start setup to be meaningful, which doesn't fit this class's
    // repeated-loop job (see that file's comment for why).

    [Benchmark]
    public List<ProductGroup> NormalQuery()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var options = optionsBuilder.Options;
        var context = new ProductContext(options);
    
        var query = context.ProductGroups
           .Include(pg => pg.Products)
               .ThenInclude(p => p.Brand)
           .Include(pg => pg.Products);
           return query.ToList();
        
    }

    private static Func<ProductContext, IEnumerable<ProductGroup>> _compiled =
       EF.CompileQuery((ProductContext ctx) => ctx.ProductGroups
          .Include(pg => pg.Products)
              .ThenInclude(p => p.Brand)
          .Include(pg => pg.Products));

    [Benchmark]
    public List<ProductGroup> CompiledQuery()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductContext>();
        optionsBuilder.UseSqlServer(connectionString);
        var options = optionsBuilder.Options;

        var context = new ProductContext(options);
        return _compiled(context).ToList();
    }
}
