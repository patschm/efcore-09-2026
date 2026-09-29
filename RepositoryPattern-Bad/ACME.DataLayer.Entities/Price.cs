namespace ACME.DataLayer.Entities;

public class Price: Entity
{
    public long ProductId { get; set; }
    public double BasePrice { get; set; }
    public string? ShopName { get; set; }
    public DateTime PriceDate { get; set; }
    
    public virtual Product Product { get; set; } = null!;
}
