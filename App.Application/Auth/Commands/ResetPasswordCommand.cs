using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace App.Application.Auth.Commands;

public class ResetPasswordCommand : IRequest<string>
{
    public string Token { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, string>
{
    private readonly IAppDbContext _context;
    public ResetPasswordCommandHandler(IAppDbContext context) => _context = context;

    public async Task<string> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword)
            || request.NewPassword.Length < 6)
            throw new ArgumentException("Token hoặc mật khẩu không hợp lệ");

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var now = DateTime.UtcNow;
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > now, cancellationToken);
        if (resetToken == null)
            throw new UnauthorizedAccessException("Token không hợp lệ hoặc đã hết hạn.");

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        resetToken.MarkUsed(now);
        resetToken.User.ChangePasswordHash(BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == resetToken.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens) token.Revoke(now);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return "Đặt lại mật khẩu thành công.";
    }
}
