using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMapping;

[Table("Products", Schema ="Core")]
public class Produkt
{
    public long Id { get; set; }
    [Column("Name")]
    public string? Naam { get; set; }
    [Column("BrandId")]
    public BrandId MerkId{ get; set; }
    [Column("Image")]
    public string? Afbeelding { get; set; }
    [NotMapped]
    public string AfbeeldingUrl { get => $"https://angular-training.azureedge.net/{Afbeelding}"; }
    [ConcurrencyCheck]
    public byte[]? Timestamp { get; set; }
    public virtual Merk Merk { get; set; } = null!;
}
