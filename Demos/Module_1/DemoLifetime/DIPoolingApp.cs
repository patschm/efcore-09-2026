using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace DemoLifetime;

internal class DIPoolingApp : IHostedService
{
    // AddDbContextPool still registers ProductContext as scoped, and this hosted service is a
    // singleton, so injecting it directly here is the same captive-dependency issue as DIApp -
    // pooling alone doesn't fix it. Combining pooling with a hosted service needs
    // AddPooledDbContextFactory + IDbContextFactory<ProductContext>, like DIFactoryApp does.
    private ProductContext _context;

    public DIPoolingApp(ProductContext context)
    {
        _context = context;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var brand in _context.Brands)
        {
           // Commented out: demo currently prints nothing when run - uncomment to see the brand names
           // Console.WriteLine(brand.Name);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}