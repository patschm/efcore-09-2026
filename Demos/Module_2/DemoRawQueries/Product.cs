namespace DemoRawQueries;

public class Product
{
    
    public long Id { get; set; }
    public string? Name { get; set; }
    public long BrandId { get; set; }
    public string? Image { get; set; }
    
    public virtual Brand Brand { get; set; } = null!;
}
