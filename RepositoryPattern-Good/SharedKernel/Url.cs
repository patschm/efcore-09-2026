namespace WebShop.SharedKernel;

// A syntactically valid absolute URL - reused wherever a link, website, or image address
// shows up across bounded contexts (a shop's storefront/logo, a brand's website, a
// product's image). Generic and carries no business meaning of its own, so sharing it
// doesn't couple those contexts' domain rules together - same reasoning as Result.
public readonly record struct Url
{
    public string Value { get; }

    public Url(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            throw new ArgumentException($"'{value}' is not a valid absolute URL.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;

    // For reading optional URLs out of legacy data that predates this invariant - a malformed
    // value (e.g. "www.sony.nl" with no scheme) becomes "no URL" instead of blowing up the read.
    public static Url? TryCreate(string? value) =>
        value is not null && Uri.TryCreate(value, UriKind.Absolute, out _) ? new Url(value) : null;
}
