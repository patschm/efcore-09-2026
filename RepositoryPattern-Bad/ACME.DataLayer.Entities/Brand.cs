namespace ACME.DataLayer.Entities;

public class Brand: Entity
{
    public Brand()
    {
        Products = new HashSet<Product>();
    }

    public string? Name { get; set; }
    public string? Website { get; set; }
    
    public virtual ICollection<Product> Products { get; set; }
}
