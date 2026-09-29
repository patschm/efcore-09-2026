using System.Security.Cryptography;
using System.Text;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Ports;
using WebShop.Search.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Search.Application.Commands;

// No IUnitOfWork here: the repository's Upsert writes the vector column via raw SQL
// immediately (see IProductEmbeddingRepository), so there's no pending change to flush.
public sealed class UpsertProductEmbeddingCommandHandler(
    IEmbeddingClient embeddingClient,
    IProductEmbeddingRepository repository) : ICommandHandler<UpsertProductEmbeddingCommand>
{
    public async Task Handle(UpsertProductEmbeddingCommand command, CancellationToken cancellationToken)
    {
        var contentHash = SHA256.HashData(Encoding.UTF8.GetBytes(command.Content));

        var existing = await repository.GetByProductId(command.ProductId, cancellationToken);

        // The whole point of ContentHash: skip an embedding-model call entirely when the
        // source text hasn't actually changed since last time.
        if (existing is not null && existing.ContentUnchanged(contentHash))
            return;

        var vector = await embeddingClient.Embed(command.Content, cancellationToken);
        var embedding = existing ?? ProductEmbedding.Create(command.ProductId, vector, contentHash);
        if (existing is not null)
            embedding.Regenerate(vector, contentHash);

        await repository.Upsert(embedding, cancellationToken);
    }
}
