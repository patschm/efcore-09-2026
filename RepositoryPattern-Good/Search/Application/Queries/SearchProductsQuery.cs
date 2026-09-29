namespace WebShop.Search.Application.Queries;

public sealed record SearchProductsQuery(string Text, int TopN = 10);
