using App.Application.DTOs;
using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace App.Application.Users.Commands
{
    public record CreateRoleCommand(CreateRoleDto Role, Guid CreatedBy) : IRequest<Guid>;

    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(IAppDbContext dbContext, ILogger<CreateRoleCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                var dto = request.Role;

                // Validate
                if (string.IsNullOrWhiteSpace(dto.Name))
                    throw new ArgumentException("Tên vai trò không được để trống");

                // Check duplicate
                var exists = await _dbContext.Roles
                    .AnyAsync(r => r.Name.ToLower() == dto.Name.ToLower(), cancellationToken);

                if (exists)
                {
                    _logger.LogWarning("Attempt to create role with existing name: {Name}", dto.Name);
                    throw new InvalidOperationException("Tên vai trò đã tồn tại");
                }

                // Validate permissions
                if (dto.PermissionIds != null && dto.PermissionIds.Any())
                {
                    var validPermissions = await _dbContext.Permissions
                        .Where(p => dto.PermissionIds.Contains(p.Id))
                        .CountAsync(cancellationToken);

                    if (validPermissions != dto.PermissionIds.Count)
                        throw new InvalidOperationException("Một hoặc nhiều quyền không tồn tại");
                }

                // Create role
                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy
                };

                _dbContext.Roles.Add(role);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Assign permissions
                if (dto.PermissionIds != null && dto.PermissionIds.Any())
                {
                    var rolePermissions = dto.PermissionIds.Select(permId => new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = permId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = request.CreatedBy
                    }).ToList();

                    _dbContext.RolePermissions.AddRange(rolePermissions);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Role {RoleId} created successfully by {CreatedBy}", role.Id, request.CreatedBy);

                return role.Id;
            }
            catch (ArgumentException)
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
                _logger.LogError(ex, "Error creating role");
                throw new Exception("Có lỗi xảy ra khi tạo vai trò");
            }
        }
    }
}
