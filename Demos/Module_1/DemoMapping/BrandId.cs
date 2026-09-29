using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DemoMapping;

public record struct BrandId(long Value);

public class BrandIdConverter : ValueConverter<BrandId, long>
{
    public BrandIdConverter(): base(id=>id.Value, val=>new BrandId(val))
    {        
    }
}

