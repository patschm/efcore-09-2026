using System.Globalization;

namespace WebShop.Search.Infrastructure.Cosmos.Adapters;

// The domain's ProductEmbedding.Vector is a plain string, in pgvector's canonical text form
// ("[0.1,0.2,...]") - confirmed against the Postgres repository's raw-SQL round trip
// (CAST(embedding AS text) on read, CAST({value} AS vector(1024)) on write). Cosmos's vector
// index needs an actual JSON array of numbers, not a string, so this is the one place that
// format gets parsed and re-formatted at the Cosmos adapter boundary.
public static class EmbeddingVectorFormat
{
    public static double[] Parse(string vector) =>
        vector.Trim('[', ']')
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => double.Parse(v, CultureInfo.InvariantCulture))
            .ToArray();

    public static string Format(IReadOnlyList<double> vector) =>
        "[" + string.Join(",", vector.Select(v => v.ToString(CultureInfo.InvariantCulture))) + "]";
}
