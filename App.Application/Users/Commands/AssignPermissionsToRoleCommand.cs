using App.Domain.Entities;
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
    public record AssignPermissionsToRoleCommand(Guid RoleId, List<Guid> PermissionIds, Guid AssignedBy) : IRequest<bool>;

    public class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<AssignPermissionsToRoleCommandHandler> _logger;

        public AssignPermissionsToRoleCommandHandler(IAppDbContext dbContext, ILogger<AssignPermissionsToRoleCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                // Validate role
                var roleExists = await _dbContext.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
                if (!roleExists)
                    throw new KeyNotFoundException("Vai trò không tồn tại");

                // Validate permissions
                var validPermissions = await _dbContext.Permissions
                    .Where(p => request.PermissionIds.Contains(p.Id))
                    .CountAsync(cancellationToken);

                if (validPermissions != request.PermissionIds.Count)
                    throw new InvalidOperationException("Một hoặc nhiều quyền không tồn tại");

                // Remove existing permissions
                var existingPermissions = await _dbContext.RolePermissions
                    .Where(rp => rp.RoleId == request.RoleId)
                    .ToListAsync(cancellationToken);

                _dbContext.RolePermissions.RemoveRange(existingPermissions);

                // Add new permissions
                var newRolePermissions = request.PermissionIds.Select(permId => new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = request.RoleId,
                    PermissionId = permId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = request.AssignedBy
                }).ToList();

                _dbContext.RolePermissions.AddRange(newRolePermissions);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Assigned {Count} permissions to role {RoleId} by {AssignedBy}",
                    request.PermissionIds.Count, request.RoleId, request.AssignedBy);

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
                throw;
            }
        }
    }
}
