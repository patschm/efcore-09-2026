using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;

namespace ACME.DataLayer.Repository.SqlServer;

public class PriceRepository : BaseRepository<Price>, IPriceRepository
{
    public PriceRepository(ShopDatabaseContext context) : base(context)
    {
    }
}
