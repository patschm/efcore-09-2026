using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace DemoLifetime;

internal class DIFactoryApp : IHostedService
{
    private IDbContextFactory<ProductContext> _contextFactory;

    public DIFactoryApp(IDbContextFactory<ProductContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using (var productContext = _contextFactory.CreateDbContext())
        {
            foreach (var brand in productContext.Brands)
            {
                Console.WriteLine(brand.Name);
            }
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}