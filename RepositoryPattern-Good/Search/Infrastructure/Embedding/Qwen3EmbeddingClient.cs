using System.Globalization;
using System.Net.Http.Json;
using WebShop.Search.Domain.Ports;

namespace WebShop.Search.Infrastructure.Embedding;

// Calls the OpenAI-compatible /v1/embeddings endpoint of the model server in
// Search/EmbeddingServer - see that folder's README for what runs on the other end of
// HttpClient.BaseAddress. Returns the same "[0.1,0.2,...]" pgvector literal format
// IProductEmbeddingRepository.Upsert already expects, so nothing downstream needed to change.
public sealed class Qwen3EmbeddingClient(HttpClient httpClient) : IEmbeddingClient
{
    public async Task<string> Embed(string text, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/v1/embeddings", new { input = text }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
        var vector = result?.Data.FirstOrDefault()?.Embedding
            ?? throw new InvalidOperationException("Embedding server returned no data.");

        return "[" + string.Join(",", vector.Select(v => v.ToString("F6", CultureInfo.InvariantCulture))) + "]";
    }

    private sealed record EmbeddingResponse(List<EmbeddingData> Data);
    private sealed record EmbeddingData(List<double> Embedding);
}
