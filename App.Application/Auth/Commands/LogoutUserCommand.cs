using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace App.Application.Auth.Commands
{
    public record LogoutUserCommand(string RefreshToken, Guid UserId) : IRequest<bool>;

    public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, bool>

    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<LogoutUserCommandHandler> _logger;

        public LogoutUserCommandHandler(
          IAppDbContext dbContext,
          ILogger<LogoutUserCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(LogoutUserCommand request, CancellationToken cancellation)
        {
            try
            {
                // 1. Validate input
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    _logger.LogWarning("Logout attempt with empty refresh token for user {UserId}", request.UserId);
                    return false;
                }

                // 2. Tìm và revoke refresh token của user
                var refreshToken = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(
                        rt => rt.Token == request.RefreshToken
                           && rt.UserId == request.UserId
                           && rt.RevokedAt == null,
                        cancellation);

                if (refreshToken == null)
                {
                    _logger.LogWarning("Refresh token not found or already revoked for user {UserId}", request.UserId);
                    // Vẫn return true vì mục đích là logout, token không tồn tại = đã logout rồi
                    return true;
                }

                // 3. Revoke refresh token
                refreshToken.RevokedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellation);

                _logger.LogInformation("User {UserId} logged out successfully", request.UserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", request.UserId);
                throw new Exception("Có lỗi xảy ra khi đăng xuất. Vui lòng thử lại");
            }
        }
    }
}
