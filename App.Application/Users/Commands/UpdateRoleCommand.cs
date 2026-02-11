using App.Application.DTOs;
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
    // UPDATE ROLE
    public record UpdateRoleCommand(Guid RoleId, UpdateRoleDto Role, Guid UpdatedBy) : IRequest<bool>;

    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        public UpdateRoleCommandHandler(IAppDbContext dbContext, ILogger<UpdateRoleCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                var dto = request.Role;

                // Get role
                var role = await _dbContext.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

                if (role == null)
                {
                    _logger.LogWarning("Attempt to update non-existent role: {RoleId}", request.RoleId);
                    throw new KeyNotFoundException("Vai trò không tồn tại");
                }

                var changes = new List<string>();

                // Update name
                if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != role.Name)
                {
                    var exists = await _dbContext.Roles
                        .AnyAsync(r => r.Name.ToLower() == dto.Name.ToLower() && r.Id != request.RoleId, cancellationToken);

                    if (exists)
                        throw new InvalidOperationException("Tên vai trò đã tồn tại");

                    changes.Add($"Name: {role.Name} -> {dto.Name}");
                    role.Name = dto.Name.Trim();
                }

                // Update description
                if (dto.Description != null && dto.Description != role.Description)
                {
                    changes.Add($"Description updated");
                    role.Description = dto.Description.Trim();
                }

                role.UpdatedAt = DateTime.UtcNow;
                role.UpdatedBy = request.UpdatedBy;

                // Update permissions
                if (dto.PermissionIds != null)
                {
                    // Validate permissions
                    var validPermissions = await _dbContext.Permissions
                        .Where(p => dto.PermissionIds.Contains(p.Id))
                        .CountAsync(cancellationToken);

                    if (validPermissions != dto.PermissionIds.Count)
                        throw new InvalidOperationException("Một hoặc nhiều quyền không tồn tại");

                    // Remove old permissions
                    _dbContext.RolePermissions.RemoveRange(role.RolePermissions);

                    // Add new permissions
                    var newPermissions = dto.PermissionIds.Select(permId => new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = permId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = request.UpdatedBy
                    }).ToList();

                    _dbContext.RolePermissions.AddRange(newPermissions);
                    changes.Add($"Permissions updated: {dto.PermissionIds.Count} permissions");
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Role {RoleId} updated successfully by {UpdatedBy}. Changes: {Changes}",
                    request.RoleId, request.UpdatedBy, string.Join(", ", changes));

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
                _logger.LogError(ex, "Error updating role {RoleId}", request.RoleId);
                throw new Exception("Có lỗi xảy ra khi cập nhật vai trò");
            }
        }
    }
}
