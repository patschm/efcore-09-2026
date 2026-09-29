using ACME.DataLayer.Entities;

namespace ACME.DataLayer.Interfaces;

public interface IBrandRepository : IRepository<Brand>
{
    Task<IEnumerable<Product>> GetProductsAsync(long brandID, int page=1, int count=10);
}
