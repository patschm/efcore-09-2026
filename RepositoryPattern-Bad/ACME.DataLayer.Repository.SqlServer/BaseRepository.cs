using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ACME.DataLayer.Repository.SqlServer;

public class BaseRepository<T> : IRepository<T> where T : Entity
{
    private readonly ShopDatabaseContext  _context;
    public BaseRepository(ShopDatabaseContext context)
    {
        _context = context;
    }
    protected ShopDatabaseContext Context { get => _context; }  
    public async Task DeleteAsync(T entity)
    {
        var dbEntity = await Context.FindAsync<T>(entity.Id);
        if (dbEntity == null) throw new Exception($"Entity {typeof(T).Name} with Id {entity.Id} not found");
        Context.Remove(dbEntity);
    }
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
    {
       return await Context.Set<T>().Where(expression).ToListAsync();
    }
    public async Task<IEnumerable<T>> GetAllAsync(int page = 1, int count = 10)
    {
        return await Context.Set<T>().Skip((page - 1) * count).Take(count).ToListAsync();
    }
    public async Task<T> GetByIdAsync(long id)
    {
        var entity = await Context.FindAsync<T>(id);
        if (entity == null) throw new Exception($"Entity {typeof(T).Name} with Id {id} not found");
        return entity;
    }
    public async Task InsertAsync(T entity)
    {
        await Context.Set<T>().AddAsync(entity);
    }
    public async Task UpdateAsync(T entity)
    {
        var dbEntity = await Context.FindAsync<T>(entity.Id);
        if (dbEntity == null) throw new Exception($"Entity {typeof(T).Name} with Id {entity.Id} not found");
        Context.Entry<T>(dbEntity).CurrentValues.SetValues(entity);
    }
    public async Task<int> SaveAsync()
    {
        return await Context.SaveChangesAsync();
    }
}
