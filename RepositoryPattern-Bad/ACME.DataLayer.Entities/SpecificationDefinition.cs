namespace ACME.DataLayer.Entities;

public class SpecificationDefinition: Entity
{
    public string? Key { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? Unit { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
    public long ProductGroupId { get; set; }
    
    public virtual ProductGroup ProductGroup { get; set; } = null!;
}
