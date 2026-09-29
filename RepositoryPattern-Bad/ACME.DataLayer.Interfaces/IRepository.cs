using ACME.DataLayer.Entities;
using System.Linq.Expressions;

namespace ACME.DataLayer.Interfaces;

public interface IRepository<T> where T: Entity
{
    Task<T> GetByIdAsync(long id);
    Task<IEnumerable<T>> GetAllAsync(int page=1, int count = 10);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);
    Task InsertAsync(T entity);
    Task DeleteAsync(T entity);
    Task UpdateAsync(T entity);
    Task<int> SaveAsync();
}