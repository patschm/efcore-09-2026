using WebShop.Search.Domain.Ports;

namespace WebShop.Search.Api.Tests.Fakes;

// Deterministic (same text -> same vector) and dependency-free, standing in for a real call to
// Search/EmbeddingServer - these tests shouldn't need a model server actually running.
internal sealed class FakeEmbeddingClient : IEmbeddingClient
{
    public Task<string> Embed(string text, CancellationToken cancellationToken)
    {
        var vector = new double[1024];
        foreach (var word in text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            vector[Math.Abs(word.GetHashCode()) % 1024] += 1;

        if (vector.All(v => v == 0))
            vector[0] = 1;

        var literal = "[" + string.Join(",", vector) + "]";
        return Task.FromResult(literal);
    }
}
