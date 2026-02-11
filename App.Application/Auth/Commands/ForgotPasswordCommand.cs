using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace App.Application.Auth.Commands
{
    public record ForgotPasswordCommand : IRequest<Unit>
    {
        public string Email { get; set; }
    }
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
    {
        private readonly IAppDbContext _context;
        private readonly IConfiguration _config;
        public ForgotPasswordCommandHandler(IAppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellation)
        {
            // tìm ng dùng
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellation);

            if (user == null)
            {
                return Unit.Value;
            }

            //tạo token reset ngẫu nhiên
            var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            // hash token để lưu vào db 
            using var sha = SHA256.Create();
            var hashedToken = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(resetToken)));

            user.ResetToken = hashedToken;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync(cancellation);

            // 🔹 Tạo link reset (FE sẽ nhận token và reset password)
            var frontendUrl = _config["App:FrontendUrl"];
            var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";

            // Gửi email (logic này nên được tách ra một service riêng)
            await SendResetEmailAsync(user, resetLink);

            // Trả về Unit để báo hiệu hoàn thành
            return Unit.Value;
             
        }



        private async Task SendResetEmailAsync(User user, string resetLink)
        {
            var subject = "Yêu cầu đặt lại mật khẩu";
            var body = $@"
            <h3>Xin chào {user.Fullname ?? user.Email},</h3>
            <p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng bấm vào liên kết dưới đây để đổi mật khẩu:</p>
            <p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>
            <p>Liên kết này sẽ hết hạn sau 1 giờ.</p>
            <p>Trân trọng,<br/>Hệ thống eStudy</p>";

            var smtpHost = _config["Smtp:Host"];
            // ✅ FIX: Dùng TryParse để an toàn
            if (!int.TryParse(_config["Smtp:Port"], out var smtpPort))
            {
                smtpPort = 587; // Giá trị mặc định an toàn
            }
            var smtpUser = _config["Smtp:User"];
            var smtpPass = _config["Smtp:Pass"];
            var fromEmail = _config["Smtp:From"];

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mail = new MailMessage(fromEmail, user.Email, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mail);
        }
    } 
}
