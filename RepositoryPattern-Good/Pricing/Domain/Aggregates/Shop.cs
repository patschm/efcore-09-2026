using WebShop.Pricing.Domain.Identifiers;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Domain.Aggregates;

public class Shop
{
    private string _name = null!;
    private double _rating;

    public ShopId Id { get; private set; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Shop name is required.", nameof(value));

            _name = value;
        }
    }

    public Url Url { get; set; }
    public Url? Logo { get; set; }

    public double Rating
    {
        get => _rating;
        set
        {
            if (value is < 0 or > 5)
                throw new ArgumentOutOfRangeException(nameof(value), "Rating must be between 0 and 5.");

            _rating = value;
        }
    }

    private Shop()
    {
    }

    public static Shop Create(ShopId id, string name, Url url, Url? logo = null, double rating = 0) =>
        new()
        {
            Id = id,
            Name = name,
            Url = url,
            Logo = logo,
            Rating = rating
        };
}
