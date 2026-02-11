using App.Application.DTOs;
using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace App.Application.Users.Commands
{

    public record CreateUserCommand(CreateUserDto User, Guid CreatedBy) : IRequest<Guid>;

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IAppDbContext dbContext,
            ILogger<CreateUserCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                var dto = request.User;

                // validate input method
                ValidateUserInput(dto);

                // 2. Check duplicate email
                var emailExists = await _dbContext.Users
                    .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower(), cancellationToken);

                if (emailExists)
                {
                    throw new InvalidOperationException("Email đã được sử dụng");
                }


                // 4. Check duplicate phone (nếu có)
                if (!string.IsNullOrWhiteSpace(dto.Phone))
                {
                    var phoneExists = await _dbContext.Users
                        .AnyAsync(u => u.Phone == dto.Phone, cancellationToken);

                    if (phoneExists)
                    {
                        throw new InvalidOperationException("Số điện thoại đã được sử dụng");
                    }
                }

                // 5. Validate roles (nếu có)
                if (dto.RoleIds != null && dto.RoleIds.Any())
                {
                    var validRoles = await _dbContext.Roles
                        .Where(r => dto.RoleIds.Contains(r.Id))
                        .CountAsync(cancellationToken);

                    if (validRoles != dto.RoleIds.Count)
                    {
                        throw new InvalidOperationException("Một hoặc nhiều Role không tồn tại");
                    }
                }

                // 6. Create user entity
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = dto.Email.ToLower().Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Fullname = dto.Fullname?.Trim(),
                    Phone = dto.Phone?.Trim(),
                    IsActive = true,
                    FailedLoginAttempts = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return user.Id;
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw ;
            }
        }

        private void ValidateUserInput(CreateUserDto dto)
        {
            var errors = new List<string>();

            // Email validation
            if (string.IsNullOrWhiteSpace(dto.Email))
                errors.Add("Email không được để trống");
            else if (!IsValidEmail(dto.Email))
                errors.Add("Email không hợp lệ");

            // Username validation
            if (string.IsNullOrWhiteSpace(dto.Fullname))
                errors.Add("Username không được để trống");
            else if (dto.Fullname.Length > 50)
                errors.Add("Username không được vượt quá 50 ký tự");
            else if (!IsValidUsername(dto.Fullname))
                errors.Add("Username chỉ được chứa chữ cái, số và dấu gạch dưới");

            // Password validation
            if (string.IsNullOrWhiteSpace(dto.Password))
                errors.Add("Mật khẩu không được để trống");
           

            // Phone validation (optional)
            if (!string.IsNullOrWhiteSpace(dto.Phone) && !IsValidPhone(dto.Phone))
                errors.Add("Số điện thoại không hợp lệ");

            // Fullname validation (optional)
            if (!string.IsNullOrWhiteSpace(dto.Fullname) && dto.Fullname.Length > 100)
                errors.Add("Họ tên không được vượt quá 100 ký tự");

            if (errors.Any())
                throw new ArgumentException(string.Join("; ", errors));
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


        private bool IsValidUsername(string username)
        {
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
        }

        private bool IsStrongPassword(string password)
        {
            // Ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt
            return Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]");
        }

        private bool IsValidPhone(string phone)
        {
            // Vietnamese phone number: 10-11 digits, start with 0
            return Regex.IsMatch(phone, @"^0\d{9,10}$");
        }
    }
}
