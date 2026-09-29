using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMapping;

[Table("Brands", Schema = "Core")]
public class Merk
{
    public Merk()
    {
        Produkten = new HashSet<Produkt>();
    }
    [Key]
    public BrandId Id { get; set; }
    [Column("Name")]
    public string? Naam { get; set; }
    public string? Website { get; set; }
    [ConcurrencyCheck]
    public byte[]? Timestamp { get; set; }
    [InverseProperty("Merk")]
    public virtual ICollection<Produkt> Produkten { get; set; }
}
