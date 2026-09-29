using System.Net;
using Microsoft.Azure.Cosmos;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Repositories;
using WebShop.Reviews.Infrastructure.Cosmos.Adapters;
using WebShop.Reviews.Infrastructure.Cosmos.Documents;

namespace WebShop.Reviews.Infrastructure.Cosmos.Persistence;

public sealed class ReviewUserRepository(CosmosClient client, CosmosOptions options, CosmosChangeTracker changeTracker) : IReviewUserRepository
{
    private const string Type = "ReviewUser";
    private static readonly PartitionKey PartitionKeyValue = new(Type);

    private Container Container => client.GetContainer(options.DatabaseName, CosmosContainers.Reference);

    public async Task<ReviewUser?> GetById(ReviewUserId id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Container.ReadItemAsync<ReviewUserDocument>(
                ReviewUserDocument.BuildId(id.Value), PartitionKeyValue, cancellationToken: cancellationToken);
            return ReviewUserDocumentAdapter.ToDomain(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ReviewUser>> GetByIds(IReadOnlyCollection<ReviewUserId> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];

        var idValues = ids.Select(id => ReviewUserDocument.BuildId(id.Value)).ToArray();
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND ARRAY_CONTAINS(@ids, c.id)")
            .WithParameter("@type", Type)
            .WithParameter("@ids", idValues);

        var results = new List<ReviewUser>();
        using var iterator = Container.GetItemQueryIterator<ReviewUserDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = PartitionKeyValue });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(ReviewUserDocumentAdapter.ToDomain));
        }

        return results;
    }

    public void Add(ReviewUser reviewUser)
    {
        var document = ReviewUserDocumentAdapter.ToDocument(reviewUser);
        changeTracker.StageUpsert(CosmosContainers.Reference, PartitionKeyValue, document.Id, document);
    }
}
