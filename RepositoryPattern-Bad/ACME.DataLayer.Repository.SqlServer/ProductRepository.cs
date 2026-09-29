using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ACME.DataLayer.Repository.SqlServer;

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(ShopDatabaseContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Price>> GetPricesAsync(long productId, int page = 1, int count = 50)
    {
        return await Context.Prices
           .Where(p => p.ProductId == productId)
           .Skip((page - 1) * count)
           .Take(count)
           .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetReviewsAsync(long productId, int page = 1, int count = 50)
    {
        return await Context.Reviews
           .Where(p => p.ProductId == productId)
           .Skip((page - 1) * count)
           .Take(count)
           .ToListAsync();
    }

    public async Task<IEnumerable<Specification>> GetSpecificationsAsync(long productId, int page = 1, int count = 50)
    {
        return await Context.Specifications
           .Where(p => p.ProductId == productId)
           .Skip((page - 1) * count)
           .Take(count)
           .ToListAsync();
    }
}
