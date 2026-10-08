using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace App.Application.Users.Commands;

public record CreateUserCommand(CreateUserDto User, Guid CreatedBy) : IRequest<Guid>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IAppDbContext _dbContext;

    public CreateUserCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.User;
        if (string.IsNullOrWhiteSpace(dto.Email) || !MailAddress.TryCreate(dto.Email.Trim(), out var address)
            || address.Address != dto.Email.Trim())
            throw new ArgumentException("Email không hợp lệ");
        if (string.IsNullOrWhiteSpace(dto.Fullname) || dto.Fullname.Trim().Length > 100)
            throw new ArgumentException("Họ tên không hợp lệ");
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            throw new ArgumentException("Mật khẩu không được để trống");
        if (!string.IsNullOrWhiteSpace(dto.Phone) && !Regex.IsMatch(dto.Phone.Trim(), @"^0\d{9,10}$"))
            throw new ArgumentException("Số điện thoại không hợp lệ");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken))
            throw new InvalidOperationException("Email đã được sử dụng");
        var phone = dto.Phone?.Trim();
        if (!string.IsNullOrWhiteSpace(phone)
            && await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Phone == phone, cancellationToken))
            throw new InvalidOperationException("Số điện thoại đã được sử dụng");

        var user = new User(email, dto.Fullname, dto.Role)
        {
            Id = Guid.NewGuid(),
            CreatedBy = request.CreatedBy
        };
        user.UpdateProfile(email, dto.Fullname, phone);
        user.ChangePasswordHash(BCrypt.Net.BCrypt.HashPassword(dto.Password));
        if (dto.Role == UserRole.Student)
        {
            if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                throw new ArgumentOutOfRangeException(nameof(dto.DateOfBirth), "Ngày sinh không thể ở tương lai");
            user.StudentProfile = new Student
            {
                UserId = user.Id,
                Fullname = user.FullName,
                DateOfBirth = dto.DateOfBirth,
                AvatarUrl = dto.AvatarUrl,
                MemberLevel = MemberLevel.Standard
            };
        }
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
