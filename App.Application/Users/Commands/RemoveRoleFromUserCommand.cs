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
    public record RemoveRoleFromUserCommand(Guid UserId, Guid RoleId, Guid RemovedBy) : IRequest<bool>;

    public class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<RemoveRoleFromUserCommandHandler> _logger;

        public RemoveRoleFromUserCommandHandler(IAppDbContext dbContext, ILogger<RemoveRoleFromUserCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userRole = await _dbContext.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);

                if (userRole == null)
                {
                    _logger.LogWarning("UserRole not found: UserId={UserId}, RoleId={RoleId}", request.UserId, request.RoleId);
                    throw new KeyNotFoundException("Vai trò không được gán cho người dùng này");
                }

                _dbContext.UserRoles.Remove(userRole);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Removed role {RoleId} from user {UserId} by {RemovedBy}",
                    request.RoleId, request.UserId, request.RemovedBy);

                return true;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);
                throw new Exception("Có lỗi xảy ra khi gỡ vai trò khỏi người dùng");
            }
        }
    }
}
