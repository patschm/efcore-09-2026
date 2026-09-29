namespace ACME.DataLayer.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IBrandRepository Brands { get; }
    IPriceRepository Prices { get; }
    IProductGroupRepository ProductGroups { get; }
    IProductRepository Products { get; }
    IReviewRepository Reviews { get; }
    ISpecificationRepository Specifications { get; }
    ISpecificationDefinitionRepository SpecificationsDefinition { get; }

    Task<int> SaveAsync();
}
