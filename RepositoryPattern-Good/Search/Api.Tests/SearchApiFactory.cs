using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebShop.Search.Api.Tests.Fakes;
using WebShop.Search.Domain.Ports;
using WebShop.Search.Domain.Repositories;

namespace WebShop.Search.Api.Tests;

// Unlike Catalog/Pricing/Reviews, this can't swap in a SQLite-backed real repository: every
// ProductEmbeddingRepository operation is either raw pgvector-specific SQL (Postgres) or a native
// Cosmos vector query - SQLite has no equivalent for either, the same reason
// Search.Infrastructure.Tests stays minimal. A fake is the only workable substitute here.
// IEmbeddingClient is also faked - Program.cs now wires up Qwen3EmbeddingClient, a real HTTP
// call to Search/EmbeddingServer, which these tests have no business depending on.
public sealed class SearchApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProductEmbeddingRepository>();
            services.RemoveAll<IEmbeddingClient>();

            services.AddSingleton<IProductEmbeddingRepository, FakeProductEmbeddingRepository>();
            services.AddSingleton<IEmbeddingClient, FakeEmbeddingClient>();
        });
    }
}
