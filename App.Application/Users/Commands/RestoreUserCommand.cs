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
    public record RestoreUserCommand(Guid UserId, Guid RestoredBy) : IRequest<bool>;

    public class RestoreUserCommandHandler : IRequestHandler<RestoreUserCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<RestoreUserCommandHandler> _logger;

        public RestoreUserCommandHandler(
            IAppDbContext dbContext,
            ILogger<RestoreUserCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Get deleted user
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (user == null)
                {
                    throw new KeyNotFoundException("Người dùng không tồn tại hoặc chưa bị xóa");
                }

                // 2. Restore user
                user.IsActive = true; // Kích hoạt lại account
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = request.RestoredBy;

                await _dbContext.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Có lỗi xảy ra khi khôi phục người dùng");
            }
        }
    }
}
