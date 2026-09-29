using WebShop.Search.Domain.Ports;

namespace WebShop.Search.Application.Tests.Fakes;

internal sealed class FakeEmbeddingClient : IEmbeddingClient
{
    public string VectorToReturn { get; set; } = "[0.1]";
    public string? LastText { get; private set; }
    public int CallCount { get; private set; }

    public Task<string> Embed(string text, CancellationToken cancellationToken)
    {
        LastText = text;
        CallCount++;
        return Task.FromResult(VectorToReturn);
    }
}
