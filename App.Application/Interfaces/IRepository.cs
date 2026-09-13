using System.Linq.Expressions;
namespace App.Application.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> Table { get; }

        // Select
        Task<TEntity?> SelectById(Guid id, CancellationToken cancellationToken = default);  
        Task<TEntity?> SelectOne(Expression<Func<TEntity, bool>> where, CancellationToken cancellationToken = default);
        Task<IList<TEntity>> Select(Expression<Func<TEntity, bool>>? where = null, CancellationToken cancellation = default);
        Task<(IList<TEntity> Items, int Total)> SelectPaged(
           Expression<Func<TEntity, bool>>? where = null,
           int page = 0, int pageSize = 20, CancellationToken ct = default);

        // ---- CHECK ----
        Task<bool> Exists(Expression<Func<TEntity, bool>> where, CancellationToken ct = default);
        Task<int> Count(Expression<Func<TEntity, bool>>? where = null, CancellationToken ct = default);


        // ---- CRUD ----
        Task Insert(TEntity entity, CancellationToken ct = default);
        Task InsertMany(IList<TEntity> entities, CancellationToken ct = default);
        Task Update(TEntity entity, CancellationToken ct = default);
        Task Delete(TEntity entity, CancellationToken ct = default);
        Task<int> DeleteWhere(Expression<Func<TEntity, bool>> where, CancellationToken ct = default);
    }
}
