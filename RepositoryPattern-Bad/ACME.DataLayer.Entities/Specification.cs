namespace ACME.DataLayer.Entities;

public class Specification: Entity
{
    public string? Key { get; set; }
    public bool? BoolValue { get; set; }
    public string? StringValue { get; set; }
    public double? NumberValue { get; set; }
    public long ProductId { get; set; }
    
    public virtual Product Product { get; set; } = null!;
}
