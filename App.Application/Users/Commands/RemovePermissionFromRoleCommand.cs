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
    // REMOVE PERMISSION FROM ROLE
    public record RemovePermissionFromRoleCommand(Guid RoleId, Guid PermissionId, Guid RemovedBy) : IRequest<bool>;

    public class RemovePermissionFromRoleCommandHandler : IRequestHandler<RemovePermissionFromRoleCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        public RemovePermissionFromRoleCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var rolePermission = await _dbContext.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == request.RoleId 
                    && rp.PermissionId == request.PermissionId, cancellationToken);

                if (rolePermission == null)
                {
                    throw new KeyNotFoundException("Quyền không được gán cho vai trò này");
                }

                _dbContext.RolePermissions.Remove(rolePermission);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Có lỗi xảy ra khi gỡ quyền khỏi vai trò");
            }
        }
    }
}
