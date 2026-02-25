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
using App.Application.Services.Interface;
using App.Domain.Entities;

namespace App.Infrastructure.Shares
{
    /// <summary>
    /// Lớp DbContext cơ sở cung cấp các tính năng tự động hóa: 
    /// Soft Delete, Global Query Filter và Audit Logging (CreatedAt, CreatedBy...).
    /// </summary>
    /// <typeparam name="TDbContext">Kiểu của DbContext cụ thể.</typeparam>
    public abstract class BaseDbContext<TDbContext> : DbContext where TDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;

        /// <summary>
        /// Khởi tạo một đối tượng BaseDbContext.
        /// </summary>
        /// <param name="options">Các tùy chọn cấu hình DbContext.</param>
        /// <param name="currentUserService">Dịch vụ lấy thông tin người dùng hiện tại.</param>
        protected BaseDbContext(DbContextOptions<TDbContext> options, ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Lấy ID của người dùng đang thực hiện thao tác.
        /// </summary>
        /// <returns>Guid của người dùng hoặc null nếu không xác định được.</returns>
        protected virtual Guid? GetCurrentUserId() => _currentUserService?.UserId;


        /// <summary>
        /// Cấu hình các ràng buộc dữ liệu khi khởi tạo Model.
        /// Thực hiện quét và tự động áp dụng Global Query Filter cho các Entity có Soft Delete.
        /// </summary>
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

        /// <summary>
        /// Hàm Generic để cấu hình Query Filter cho tính năng xóa mềm (Soft Delete).
        /// Đảm bảo các truy vấn mặc định sẽ bỏ qua dữ liệu đã bị xóa.
        /// </summary>
        public void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class, ISoftDelete
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(x => !x.IsDeleted);
        }


        /// <summary>
        /// Lưu trữ Metadata của hàm SetSoftDeleteFilter để tối ưu hóa việc gọi bằng Reflection.
        /// </summary>
        static readonly MethodInfo SetSoftDeleteFilterMethod = typeof(BaseDbContext<TDbContext>)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(t => t.IsGenericMethod && t.Name == "SetSoftDeleteFilter");


        // ---------------------------------------------------------
        // PHẦN 2: XỬ LÝ KHI SAVE (Logic cũ của bạn)
        // ---------------------------------------------------------

        /// <summary>
        /// Ghi đè phương thức lưu thay đổi (Async). 
        /// Tự động thực hiện logic Tracking (Audit) trước khi lưu vào Database.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(cancellationToken);
        }


        /// <summary>
        /// Ghi đè phương thức lưu thay đổi (Sync). 
        /// Tự động thực hiện logic Tracking (Audit) trước khi lưu vào Database.
        /// </summary>
        public override int SaveChanges()
        {
            OnBeforeSaving();
            return base.SaveChanges();
        }

        #region Transaction Support
        /// <summary>
        /// Bắt đầu một Transaction mới.
        /// </summary>
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await Database.BeginTransactionAsync(cancellationToken);
        }


        /// <summary>
        /// Xác nhận và hoàn tất Transaction hiện tại.
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await Database.CommitTransactionAsync(cancellationToken);
        }


        /// <summary>
        /// Hủy bỏ và khôi phục lại dữ liệu nếu Transaction gặp lỗi.
        /// </summary>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            await Database.RollbackTransactionAsync(cancellationToken);
        }
        #endregion


        #region Helper Methods
        /// <summary>
        /// Hỗ trợ tạo truy vấn nhanh cho một Entity.
        /// </summary>
        /// <param name="asNoTracking">Có sử dụng NoTracking để tăng hiệu suất truy vấn hay không.</param>
        public IQueryable<TEntity> Query<TEntity>(bool asNoTracking = false) where TEntity : class
        {
            var query = Set<TEntity>().AsQueryable();
            return asNoTracking ? query.AsNoTracking() : query;
        }

        /// <summary>
        /// Làm sạch bộ nhớ đệm của ChangeTracker.
        /// </summary>
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

        #endregion

        /// <summary>
        /// Xử lý logic nghiệp vụ trước khi dữ liệu được ghi xuống DB:
        /// 1. Tự động gán thời gian và người thực hiện (Audit Tracking).
        /// 2. Chuyển đổi trạng thái Deleted sang trạng thái ẩn (Soft Delete).
        /// </summary>
        private void OnBeforeSaving()
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                // --- XỬ LÝ TRACKING (Kế thừa từ BaseEntity) ---
                if (entry.Entity is BaseEntity baseEntity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            baseEntity.CreatedAt = now;
                            baseEntity.CreatedBy = userId;
                            baseEntity.IsDeleted = false;
                            break;

                        case EntityState.Modified:
                            entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                            entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;

                            baseEntity.UpdatedAt = now;
                            baseEntity.UpdatedBy = userId;
                            break;

                        case EntityState.Deleted:
                            entry.State = EntityState.Modified;

                            baseEntity.IsDeleted = true;
                            baseEntity.DeletedAt = now;
                            baseEntity.DeletedBy = userId;

                            baseEntity.UpdatedAt = now;
                            baseEntity.UpdatedBy = userId;
                            break;
                    }
                }
                // --- XỬ LÝ CHO CÁC ENTITY CHỈ CÓ ISoftDelete (nếu có) ---
                else if (entry.Entity is ISoftDelete softDeleteEntity && entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    softDeleteEntity.IsDeleted = true;
                    softDeleteEntity.DeletedAt = now;
                }
            }
        }
    }
}
