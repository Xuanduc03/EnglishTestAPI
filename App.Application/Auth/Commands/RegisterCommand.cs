using App.Application.DTOs;
using App.Domain.Entities;
using App.Application.Interfaces;
using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace App.Application.Auth.Commands;

public record RegisterUserCommand(RegisterDto User) : IRequest<Guid>;
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IAppDbContext _dbContext;
    public RegisterUserCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.User;

        // 1. validate input 
        ValidateRegistation(dto);

        //2. check mail and username exist
        var emailExists = await _dbContext.Users
            .AnyAsync(u => u.Email == dto.Email.ToLower(), cancellationToken);

        if(emailExists)
        {
            throw new InvalidOperationException("Email đã được sử dụng");
        }

        var userId = Guid.NewGuid();
        // 3. create new user
        var user = new User
        {
            Id = userId,
            Email = dto.Email.ToLower().Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Fullname = dto.Fullname,
            EmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        // lay role mac dinh
        var defaultRole = await _dbContext.Roles
                        .FirstOrDefaultAsync(r => r.Name.ToLower() == "student", cancellationToken);

        if(defaultRole == null)
        {
            throw new Exception("Không có role mặc định, vui lòng cấu hình");
        }

        // create user role
        var newUserRole = new UserRole
        {
            UserId = user.Id,
            RoleId = defaultRole.Id,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = user.Id,
        };

        var newStudent = new Student
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Fullname = user.Fullname,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // 4. using transaction
        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            _dbContext.Users.Add(user);
            _dbContext.UserRoles.Add(newUserRole);
            _dbContext.Students.Add(newStudent);    
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.CommitTransactionAsync(cancellationToken);

            return user.Id;
        }
        catch (DbUpdateException ex)
        {
            // Log ex here
            await _dbContext.RollbackTransactionAsync(cancellationToken);
            throw new InvalidOperationException("Lỗi cơ sở dữ liệu khi tạo tài khoản.", ex);
        }
    }


    private void ValidateRegistation(RegisterDto dto)
    {
        var errors = new List<string>();

        // email validation
        if (string.IsNullOrWhiteSpace(dto.Email) || !IsValidEmail(dto.Email)) 
        {
            errors.Add("email không hợp lệ");
        }

        //password validation 
        if(string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            errors.Add("Mật khẩu không hợp lệ");
        }
        //username validation
        if(string.IsNullOrWhiteSpace(dto.Fullname) || dto.Fullname.Length > 50)
        {
            errors.Add("Họ và tên không hợp lệ");
        }

        if (errors.Any())
        {
            throw new ArgumentException(string.Join("; ", errors));
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}