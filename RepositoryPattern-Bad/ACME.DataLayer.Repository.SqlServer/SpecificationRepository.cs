using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;

namespace ACME.DataLayer.Repository.SqlServer;

public class SpecificationRepository : BaseRepository<Specification>, ISpecificationRepository
{
    public SpecificationRepository(ShopDatabaseContext context) : base(context)
    {
    }
}
