namespace WebShop.Search.Domain.Ports;

// Port for turning text into a vector embedding - implemented in Infrastructure against
// whichever model is actually deployed. There has only ever been one model in practice, so
// unlike an earlier version of this port, no model identifier travels back with the vector -
// see ProductEmbedding.ContentHash for what actually needs tracking (whether the source text
// has changed), which this port has no role in.
public interface IEmbeddingClient
{
    Task<string> Embed(string text, CancellationToken cancellationToken);
}
