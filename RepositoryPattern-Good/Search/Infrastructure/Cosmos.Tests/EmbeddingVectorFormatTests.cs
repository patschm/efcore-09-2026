using System.Globalization;
using WebShop.Search.Infrastructure.Cosmos.Adapters;

namespace WebShop.Search.Infrastructure.Cosmos.Tests;

public class EmbeddingVectorFormatTests
{
    [Fact]
    public void Parse_reads_a_pgvector_style_literal_into_doubles()
    {
        var values = EmbeddingVectorFormat.Parse("[0.1,-0.25,3]");

        Assert.Equal([0.1, -0.25, 3.0], values);
    }

    [Fact]
    public void Format_writes_doubles_back_into_a_pgvector_style_literal()
    {
        var literal = EmbeddingVectorFormat.Format([0.1, -0.25, 3.0]);

        Assert.Equal("[0.1,-0.25,3]", literal);
    }

    [Fact]
    public void Parse_then_Format_round_trips_a_realistic_1024_dimension_vector()
    {
        var original = "[" + string.Join(",", Enumerable.Range(0, 1024).Select(i => (i * 0.001 - 0.5).ToString(CultureInfo.InvariantCulture))) + "]";

        var roundTripped = EmbeddingVectorFormat.Format(EmbeddingVectorFormat.Parse(original));

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void Format_uses_invariant_culture_regardless_of_the_current_thread_culture()
    {
        // A comma-decimal culture (e.g. nl-NL) would otherwise turn "0.5" into "0,5", which is
        // not a valid Cosmos vector index entry - this is the one place the domain's pgvector
        // string format gets parsed/reformatted, so a culture regression here silently corrupts
        // every embedding written afterwards.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

            Assert.Equal("[0.5,1.25]", EmbeddingVectorFormat.Format([0.5, 1.25]));
            Assert.Equal([0.5, 1.25], EmbeddingVectorFormat.Parse("[0.5,1.25]"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_and_Format_handle_a_single_element_vector()
    {
        Assert.Equal([42.0], EmbeddingVectorFormat.Parse("[42]"));
        Assert.Equal("[42]", EmbeddingVectorFormat.Format([42.0]));
    }
}
