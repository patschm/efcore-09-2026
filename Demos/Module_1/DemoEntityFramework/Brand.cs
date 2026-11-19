namespace DemoEntityFramework;

public class Brand
{
    public Brand()
    {
        Products = new HashSet<Product>();
    }
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Website { get; set; }
    
    public ICollection<Product> Products { get; set; }
}
