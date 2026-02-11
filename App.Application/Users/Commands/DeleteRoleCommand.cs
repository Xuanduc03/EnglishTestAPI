using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Users.Commands
{
    // DELETE ROLE
    public record DeleteRoleCommand(Guid RoleId, Guid DeletedBy) : IRequest<bool>;

    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(IAppDbContext dbContext, ILogger<DeleteRoleCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);
            try
            {
                var role = await _dbContext.Roles
                    .Include(r => r.UserRoles)
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

                if (role == null)
                {
                    _logger.LogWarning("Attempt to delete non-existent role: {RoleId}", request.RoleId);
                    throw new KeyNotFoundException("Vai trò không tồn tại");
                }

                // Check if role is assigned to users
                if (role.UserRoles.Any())
                {
                    _logger.LogWarning("Attempt to delete role {RoleId} that is assigned to {Count} users",
                        request.RoleId, role.UserRoles.Count);
                    throw new InvalidOperationException(
                        $"Không thể xóa vai trò này vì đang được gán cho {role.UserRoles.Count} người dùng");
                }

                // Delete role permissions
                _dbContext.RolePermissions.RemoveRange(role.RolePermissions);

                // Delete role
                _dbContext.Roles.Remove(role);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Role {RoleId} deleted successfully by {DeletedBy}", request.RoleId, request.DeletedBy);

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
                _logger.LogError(ex, "Error deleting role {RoleId}", request.RoleId);
                throw new Exception("Có lỗi xảy ra khi xóa vai trò");
            }
        }
    }
}
