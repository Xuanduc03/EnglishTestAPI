using App.Application.Interfaces;
using App.Domain.Entities;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace App.Infrastructure
{
    public class EntityRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly AppDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public EntityRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public IQueryable<TEntity> Table => _dbSet.AsQueryable();

        // SELECT
        public async Task<TEntity?> SelectById(Guid id, CancellationToken ct = default) 
            => await _dbSet.FindAsync(new object[] { id }, ct);

        public async Task<TEntity?> SelectOne(Expression<Func<TEntity, bool>> where, CancellationToken ct = default)
            => await _dbSet.AsNoTracking().FirstOrDefaultAsync(where, ct);

        public async Task<IList<TEntity>> Select(Expression<Func<TEntity, bool>>? where = null, CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();
            if (where != null) query = query.Where(where);
            return await query.ToListAsync(ct);
        }

        public async Task<(IList<TEntity> Items, int Total)> SelectPaged(
             Expression<Func<TEntity, bool>>? where = null,
             int page = 0, int pageSize = 20, CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();
            if (where != null) query = query.Where(where);

            var total = await query.CountAsync(ct);
            var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }


        public async Task<bool> Exists(Expression<Func<TEntity, bool>> where, CancellationToken ct = default)
         => await _dbSet.AnyAsync(where, ct);

        public async Task<int> Count(Expression<Func<TEntity, bool>>? where = null, CancellationToken ct = default)
            => where is null ? await _dbSet.CountAsync(ct) : await _dbSet.CountAsync(where, ct);


        // CRUD
        public async Task Insert(TEntity entity, CancellationToken ct = default)
           => await _dbSet.AddAsync(entity, ct);

        public async Task InsertMany(IList<TEntity> entities, CancellationToken ct = default)
            => await _dbSet.AddRangeAsync(entities, ct);

        public Task Update(TEntity entity, CancellationToken ct = default)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public Task UpdateMany(IList<TEntity> entities, CancellationToken ct = default)
        {
            _dbSet.UpdateRange(entities);
            return Task.CompletedTask;
        }


        public Task Delete(TEntity entity, CancellationToken ct = default)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<int> DeleteWhere(Expression<Func<TEntity, bool>> where, CancellationToken ct = default)
        {
            var entities = await _dbSet.Where(where).ToListAsync(ct);
            _dbSet.RemoveRange(entities);
            return entities.Count;
        }
    }
}
