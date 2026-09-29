namespace ACME.DataLayer.Entities;

public class Product: Entity
{
    public Product()
    {
        Prices = new HashSet<Price>();
        Reviews = new HashSet<Review>();
        Specifications = new HashSet<Specification>();
    }

    public string? Name { get; set; }
    public long BrandId { get; set; }
    public long ProductGroupId { get; set; }
    public string? Image { get; set; }
    
    public virtual Brand Brand { get; set; } = null!;
    public virtual ProductGroup ProductGroup { get; set; } = null!;
    public virtual ICollection<Price> Prices { get; set; }
    public virtual ICollection<Review> Reviews { get; set; }
    public virtual ICollection<Specification> Specifications { get; set; }
}
