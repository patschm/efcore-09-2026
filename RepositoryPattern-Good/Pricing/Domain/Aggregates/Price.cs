using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.ValueObjects;

namespace WebShop.Pricing.Domain.Aggregates;

// Aggregate root in its own right: a shop's quote for a product has its own lifecycle
// (updated by a price-scraping process on its own schedule), independent of the product
// or shop it refers to. ProductId/ShopId are references to other bounded contexts'
// aggregate roots by id only - no navigation into Catalog.Product or this context's Shop.
public class Price
{
    private int _inStock;
    private Money _shippingPrice;

    public PriceId Id { get; private set; }
    public ProductId ProductId { get; private set; }
    public ShopId ShopId { get; private set; }
    public Money ShopPrice { get; set; }

    // Despite the name, this is the delivered total (ShopPrice + actual shipping cost),
    // not the shipping fee alone - the schema has no separate column for that fee.
    // Its currency must match ShopPrice's, since both describe one quote - checked here,
    // which means Create (below) must assign ShopPrice before ShippingPrice.
    public Money ShippingPrice
    {
        get => _shippingPrice;
        set
        {
            if (value.Currency != ShopPrice.Currency)
                throw new ArgumentException(
                    $"ShippingPrice currency ({value.Currency}) must match ShopPrice currency ({ShopPrice.Currency}).",
                    nameof(value));

            _shippingPrice = value;
        }
    }

    public int InStock
    {
        get => _inStock;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Stock cannot be negative.");

            _inStock = value;
        }
    }

    public Money TotalPrice => ShippingPrice;

    private Price()
    {
    }

    public static Price Create(PriceId id, ProductId productId, ShopId shopId, Money shopPrice, Money shippingPrice, int inStock) =>
        new()
        {
            Id = id,
            ProductId = productId,
            ShopId = shopId,
            ShopPrice = shopPrice,
            ShippingPrice = shippingPrice,
            InStock = inStock
        };
}
