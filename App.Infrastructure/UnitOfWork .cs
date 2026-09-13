using App.Application.Interfaces;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace App.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public UnitOfWork(AppDbContext context) => _context = context;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) => _context.BeginTransactionAsync(ct);
        public Task CommitTransactionAsync(CancellationToken ct = default) => _context.CommitTransactionAsync(ct);
        public Task RollbackTransactionAsync(CancellationToken ct = default) => _context.RollbackTransactionAsync(ct);
    }
}
