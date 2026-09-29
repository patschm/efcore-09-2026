namespace WebShop.BuildingBlocks.Cosmos;

// The two containers every bounded context shares. "Reference" holds broadly-shared master/
// taxonomy data (partitioned by /type); "Products" holds everything scoped to one product
// (partitioned by /productId), so rendering a product page is a single-partition read.
public static class CosmosContainers
{
    public const string Reference = "reference";
    public const string Products = "products";
}
