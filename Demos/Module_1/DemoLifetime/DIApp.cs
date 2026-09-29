using Microsoft.Extensions.Hosting;

namespace DemoLifetime;

internal class DIApp : IHostedService
{
    // Watch out! Single instance of ProductContext. AddHostedService is singleton
    private ProductContext _productContext;

    public DIApp(ProductContext productContext)
    {
        _productContext = productContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        
        foreach (var brand in _productContext.Brands)
        {
            Console.WriteLine(brand.Name);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}