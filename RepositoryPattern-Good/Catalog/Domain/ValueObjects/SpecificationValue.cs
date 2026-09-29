using WebShop.SharedKernel;

namespace WebShop.Catalog.Domain.ValueObjects;

// A specification value is really one of three shapes (number, text, or flag) - stored as
// three nullable columns because SQL has no union type. This Value Object centralizes the
// "exactly one must be set" rule in one place and carries no identity of its own: two
// SpecificationValues with the same contents are equal, which is what "record" gives for free.
public sealed record SpecificationValue
{
    public decimal? Number { get; }
    public string? Text { get; }
    public bool? Flag { get; }

    private SpecificationValue(decimal? number, string? text, bool? flag)
    {
        Number = number;
        Text = text;
        Flag = flag;
    }

    public static Result<SpecificationValue> Create(decimal? number = null, string? text = null, bool? flag = null)
    {
        var setCount = new[] { number.HasValue, text is not null, flag.HasValue }.Count(set => set);
        if (setCount != 1)
            return Result<SpecificationValue>.Failure("Exactly one of number, text, or flag must be set.");

        return Result<SpecificationValue>.Success(new SpecificationValue(number, text, flag));
    }
}
