using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Interfaces
{
    public interface IBaseDbContext
    {
        // Lấy dbset cho 1 entity
        DbSet<T> Set<T>() where T : class;
        // Lưu thay đổi
        Task<int> SaveChangesAsync(CancellationToken cancellation = default);
        int SaveChanges();

        // quản lý transaction
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        // Truy vấn linh hoạt
        IQueryable<TEntity> Query<TEntity>(bool asNoTracking = false) where TEntity : class;
        Task<TEntity> FindAsync<TEntity>(params object[] keyValues) where TEntity : class;
        Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class;

        void ClearChangeTracker();

        ChangeTracker ChangeTracker { get; }
        // Cấu hình và metadata
        IModel GetModel();
    }
}
