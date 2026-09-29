using ACME.DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ACME.DataLayer.Repository.SqlServer;

public class UnitOfWork : IUnitOfWork
{
    private readonly ShopDatabaseContext _context;
    private bool _disposed;

    public UnitOfWork(IDbContextFactory<ShopDatabaseContext> contextFactory)
    {
        _context = contextFactory.CreateDbContext();
        Brands = new BrandRepository(_context);
        Prices = new PriceRepository(_context);
        ProductGroups = new ProductGroupRepository(_context);
        Products = new ProductRepository(_context);
        Reviews = new ReviewRepository(_context);
        Specifications = new SpecificationRepository(_context);
        SpecificationsDefinition = new SpecificationDefinitionRepository(_context);
    }
    
    public IBrandRepository Brands { get; private set; }

    public IPriceRepository Prices { get; private set; }

    public IProductGroupRepository ProductGroups { get; private set; }

    public IProductRepository Products { get; private set; }

    public IReviewRepository Reviews { get; private set; }

    public ISpecificationRepository Specifications { get; private set; }

    public ISpecificationDefinitionRepository SpecificationsDefinition { get; private set; }

    public void Dispose()
    {
        if (_disposed) return;
        _context.Dispose();
        _disposed = true;
    }

    public async Task<int> SaveAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
