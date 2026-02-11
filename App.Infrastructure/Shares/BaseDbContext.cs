using App.Application.Interfaces;
using App.Domain.Shares;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace App.Infrastructure.Shares
{
    public abstract class BaseDbContext<TDbContext> : DbContext where TDbContext : DbContext
    {
        protected BaseDbContext(DbContextOptions<TDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Quét tất cả các Entity trong DbContext
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Nếu Entity có implement ISoftDelete
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    // Dùng Reflection để gọi hàm SetSoftDeleteFilter<T> với T là class cụ thể
                    // Điều này giúp EF Core hiểu đúng kiểu, KHÔNG BỊ LỖI ép kiểu Interface
                    var method = SetSoftDeleteFilterMethod.MakeGenericMethod(entityType.ClrType);
                    method.Invoke(this, new object[] { modelBuilder });
                }
            }
        }
        // Hàm Generic để tạo filter đúng chuẩn EF Core
        public void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class, ISoftDelete
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(x => !x.IsDeleted);
        }

        // Lấy thông tin hàm SetSoftDeleteFilter để dùng cho Reflection
        static readonly MethodInfo SetSoftDeleteFilterMethod = typeof(BaseDbContext<TDbContext>)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(t => t.IsGenericMethod && t.Name == "SetSoftDeleteFilter");


        // ---------------------------------------------------------
        // PHẦN 2: XỬ LÝ KHI SAVE (Logic cũ của bạn)
        // ---------------------------------------------------------
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var deletedEntries = ChangeTracker.Entries<ISoftDelete>()
                .Where(e => e.State == EntityState.Deleted);

            foreach (var entry in deletedEntries)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            // Gọi logic xử lý xóa mềm cho cả hàm đồng bộ (nếu cần)
            var deletedEntries = ChangeTracker.Entries<ISoftDelete>()
               .Where(e => e.State == EntityState.Deleted);

            foreach (var entry in deletedEntries)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }

            return base.SaveChanges();
        }



        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await Database.CommitTransactionAsync(cancellationToken);
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            await Database.RollbackTransactionAsync(cancellationToken);
        }
        public IQueryable<TEntity> Query<TEntity>(bool asNoTracking = false) where TEntity : class
        {
            var query = Set<TEntity>().AsQueryable();
            return asNoTracking ? query.AsNoTracking() : query;
        }

        public void ClearChangeTracker()
        {
            ChangeTracker.Clear();
        }
        public async Task<TEntity> FindAsync<TEntity>(params object[] keyValues) where TEntity : class
        {
            return await Set<TEntity>().FindAsync(keyValues);
        }

        public async Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class
        {
            return await Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);
        }
        public IModel GetModel()
        {
            return Model;
        }
    }
}
