using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace App.Application.Users.Commands;

public record UpdateUserCommand(Guid UserId, UpdateUserDto User, Guid UpdatedBy) : IRequest<bool>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IAppDbContext _dbContext;

    public UpdateUserCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.User;
        if (dto.NewPassword != null && (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6))
            throw new ArgumentException("Mật khẩu mới phải có ít nhất 6 ký tự", nameof(dto.NewPassword));
        var user = await _dbContext.Users.Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Người dùng không tồn tại");

        var email = dto.Email?.Trim().ToLowerInvariant() ?? user.Email;
        if (!MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new ArgumentException("Email không hợp lệ");
        var fullName = dto.Fullname ?? user.FullName;
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 100)
            throw new ArgumentException("Họ tên không hợp lệ");
        var phone = dto.Phone == null ? user.Phone : dto.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^0\d{9,10}$"))
            throw new ArgumentException("Số điện thoại không hợp lệ");

        if (email != user.Email && await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Id != user.Id && u.Email == email, cancellationToken))
            throw new InvalidOperationException("Email đã được sử dụng");
        if (phone != user.Phone && !string.IsNullOrWhiteSpace(phone)
            && await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Id != user.Id && u.Phone == phone, cancellationToken))
            throw new InvalidOperationException("Số điện thoại đã được sử dụng");

        var revokeTokens = (dto.Role.HasValue && dto.Role.Value != user.Role)
            || dto.IsActive == false
            || !string.IsNullOrWhiteSpace(dto.NewPassword);
        var becomesStudent = dto.Role == UserRole.Student && user.Role != UserRole.Student;

        user.UpdateProfile(email, fullName, phone);
        if (user.StudentProfile != null) user.StudentProfile.Fullname = user.FullName;
        if (dto.Role.HasValue) user.ChangeRole(dto.Role.Value);
        if (becomesStudent && user.StudentProfile == null)
        {
            var existingProfile = await _dbContext.Students.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.UserId == user.Id, cancellationToken);
            if (existingProfile == null)
            {
                user.StudentProfile = new Student { UserId = user.Id, Fullname = user.FullName };
            }
            else
            {
                existingProfile.IsDeleted = false;
                existingProfile.DeletedAt = null;
                existingProfile.DeletedBy = null;
                existingProfile.IsActive = true;
                existingProfile.Fullname = user.FullName;
                user.StudentProfile = existingProfile;
            }
        }
        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value) user.Reactivate();
            else user.Deactivate();
        }
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            user.ChangePasswordHash(BCrypt.Net.BCrypt.HashPassword(dto.NewPassword));
        }
        if (revokeTokens)
        {
            var activeTokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(cancellationToken);
            foreach (var token in activeTokens) token.Revoke(DateTime.UtcNow);
        }
        user.UpdatedBy = request.UpdatedBy;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
