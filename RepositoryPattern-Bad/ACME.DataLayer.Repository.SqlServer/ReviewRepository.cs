using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;

namespace ACME.DataLayer.Repository.SqlServer;

public class ReviewRepository : BaseRepository<Review>, IReviewRepository
{
    public ReviewRepository(ShopDatabaseContext context) : base(context)
    {
    }
}
