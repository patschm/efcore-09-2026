using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;

namespace ACME.DataLayer.Repository.SqlServer;

public class SpecificationDefinitionRepository : BaseRepository<SpecificationDefinition>, ISpecificationDefinitionRepository
{
    public SpecificationDefinitionRepository(ShopDatabaseContext context) : base(context)
    {
    }
}
