using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ACME.DataLayer.Repository.SqlServer;

public class BrandRepository : BaseRepository<Brand>, IBrandRepository
{
    public BrandRepository(ShopDatabaseContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetProductsAsync(long brandID, int page = 1, int count = 10)
    {
        return await Context.Products
            .Include(e => e.Brand)
            .Where(e => e.BrandId == brandID)
            .Skip((page - 1) * count)
            .Take(count)
            .ToListAsync();
    }
}
