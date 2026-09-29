using WebShop.Catalog.Domain.Identifiers;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Domain.Aggregates;

public class Brand
{
    private string _name = null!;

    public BrandId Id { get; private set; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Brand name is required.", nameof(value));

            _name = value;
        }
    }

    public Url? Website { get; set; }
    public Url? Logo { get; set; }

    private Brand()
    {
    }

    public static Brand Create(BrandId id, string name, Url? website = null, Url? logo = null) =>
        new()
        {
            Id = id,
            Name = name,
            Website = website,
            Logo = logo
        };
}
