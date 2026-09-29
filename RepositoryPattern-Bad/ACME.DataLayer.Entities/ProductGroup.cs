namespace ACME.DataLayer.Entities;

public class ProductGroup: Entity
{
    public ProductGroup()
    {
        Products = new HashSet<Product>();
        SpecificationDefinitions = new HashSet<SpecificationDefinition>();
    }

    public string? Name { get; set; }
    public string? Image { get; set; }
    
    public virtual ICollection<Product> Products { get; set; }
    public virtual ICollection<SpecificationDefinition> SpecificationDefinitions { get; set; }
}
