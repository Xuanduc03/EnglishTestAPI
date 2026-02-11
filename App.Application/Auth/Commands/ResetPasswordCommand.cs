using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace App.Application.Auth.Commands
{
    public class ResetPasswordCommand : IRequest<string>
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, string>
    {
        private readonly IAppDbContext _context;

        public ResetPasswordCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<string> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            // Hash lại token mà user gửi lên
            using var sha = SHA256.Create();
            var hashedToken = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(request.Token)));

            // Tìm user có token này
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ResetToken == hashedToken && u.ResetTokenExpiry > DateTime.UtcNow, cancellationToken);

            if (user == null)
                throw new UnauthorizedAccessException("Token không hợp lệ hoặc đã hết hạn.");

            // Đổi mật khẩu (ở đây nên hash mật khẩu)
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync(cancellationToken);

            return "Đặt lại mật khẩu thành công.";
        }
    }
}
