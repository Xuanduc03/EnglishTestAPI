using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Users.Commands
{
    public record DeleteUserCommand(Guid UserId, Guid DeletedBy, bool HardDelete = false) : IRequest<bool>;

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(
            IAppDbContext dbContext,
            ILogger<DeleteUserCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. Get user
                var user = await _dbContext.Users
                    .Include(u => u.UserRoles)
                    .Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (user == null)
                {
                    throw new KeyNotFoundException("Người dùng không tồn tại");
                }

                // 2. Không cho phép tự xóa chính mình
                if (user.Id == request.DeletedBy)
                {
                    throw new InvalidOperationException("Không thể xóa chính mình");
                }

                if (request.HardDelete)
                {
                    // Xóa UserRoles
                    _dbContext.UserRoles.RemoveRange(user.UserRoles);

                    // Xóa RefreshTokens
                    _dbContext.RefreshTokens.RemoveRange(user.RefreshTokens);

                    // Xóa User
                    _dbContext.Users.Remove(user);
                }
                else
                {
                    user.IsActive = false;

                    // Revoke tất cả refresh tokens
                    var activeTokens = user.RefreshTokens.Where(rt => rt.RevokedAt == null).ToList();
                    foreach (var token in activeTokens)
                    {
                        token.RevokedAt = DateTime.UtcNow;
                    }

                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (KeyNotFoundException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new Exception("Có lỗi xảy ra khi xóa người dùng");
            }
        }
    }
}