using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace App.Application.Auth.Commands;

public record RegisterUserCommand(RegisterDto User) : IRequest<Guid>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IAppDbContext _dbContext;
    public RegisterUserCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.User;
        if (string.IsNullOrWhiteSpace(dto.Email) || !MailAddress.TryCreate(dto.Email.Trim(), out var address)
            || address.Address != dto.Email.Trim())
            throw new ArgumentException("Email không hợp lệ");
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            throw new ArgumentException("Mật khẩu không hợp lệ");
        if (string.IsNullOrWhiteSpace(dto.Fullname) || dto.Fullname.Trim().Length > 100)
            throw new ArgumentException("Họ tên không hợp lệ");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken))
            throw new InvalidOperationException("Email đã được sử dụng");

        var user = new User(email, dto.Fullname, UserRole.Student);
        user.ChangePasswordHash(BCrypt.Net.BCrypt.HashPassword(dto.Password));
        var student = new Student { UserId = user.Id, Fullname = user.FullName, MemberLevel = MemberLevel.Standard };
        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);
        _dbContext.Users.Add(user);
        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user.Id;
    }
}
