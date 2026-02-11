using App.Application.DTOs;
using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace App.Application.Users.Commands
{
    public record UpdateUserCommand(Guid UserId, UpdateUserDto User, Guid UpdatedBy) : IRequest<bool>;

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IAppDbContext dbContext,
            ILogger<UpdateUserCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                var dto = request.User;

                // 1. Get existing user
                var user = await _dbContext.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Attempt to update non-existent user: {UserId}", request.UserId);
                    throw new KeyNotFoundException("Người dùng không tồn tại");
                }

                // 2. Validate input
                ValidateUserInput(dto);

                // 3. Check duplicate email (nếu thay đổi)
                if (!string.IsNullOrWhiteSpace(dto.Email) &&
                    dto.Email.ToLower() != user.Email.ToLower())
                {
                    var emailExists = await _dbContext.Users
                        .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != request.UserId,
                            cancellationToken);

                    if (emailExists)
                    {
                        _logger.LogWarning("Attempt to update user with existing email: {Email}", dto.Email);
                        throw new InvalidOperationException("Email đã được sử dụng");
                    }
                }

               

                // 5. Check duplicate phone (nếu thay đổi)
                if (!string.IsNullOrWhiteSpace(dto.Phone) && dto.Phone != user.Phone)
                {
                    var phoneExists = await _dbContext.Users
                        .AnyAsync(u => u.Phone == dto.Phone && u.Id != request.UserId,
                            cancellationToken);

                    if (phoneExists)
                    {
                        _logger.LogWarning("Attempt to update user with existing phone: {Phone}", dto.Phone);
                        throw new InvalidOperationException("Số điện thoại đã được sử dụng");
                    }
                }

                // 6. Track changes for logging
                var changes = new List<string>();

                // 7. Update user fields
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.ToLower() != user.Email.ToLower())
                {
                    changes.Add($"Email: {user.Email} -> {dto.Email}");
                    user.Email = dto.Email.ToLower().Trim();
                }

                if (!string.IsNullOrWhiteSpace(dto.Fullname) && dto.Fullname != user.Fullname)
                {
                    changes.Add($"Fullname: {user.Fullname} -> {dto.Fullname}");
                    user.Fullname = dto.Fullname.Trim();
                }

                if (dto.Phone != null && dto.Phone != user.Phone)
                {
                    changes.Add($"Phone: {user.Phone ?? "null"} -> {dto.Phone}");
                    user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
                }

                if (dto.IsActive.HasValue && dto.IsActive.Value != user.IsActive)
                {
                    changes.Add($"IsActive: {user.IsActive} -> {dto.IsActive.Value}");
                    user.IsActive = dto.IsActive.Value;
                }


                // 8. Update password (nếu có)
                if (!string.IsNullOrWhiteSpace(dto.NewPassword))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                    changes.Add("Password updated");

                    // Revoke all refresh tokens khi đổi password
                    var activeTokens = await _dbContext.RefreshTokens
                        .Where(rt => rt.UserId == request.UserId && rt.RevokedAt == null)
                        .ToListAsync(cancellationToken);

                    foreach (var token in activeTokens)
                    {
                        token.RevokedAt = DateTime.UtcNow;
                    }
                }

                user.UpdatedAt = DateTime.UtcNow;
                // 9. Update roles (nếu có)
                if (dto.RoleIds != null)
                {
                    // Validate roles
                    var validRoles = await _dbContext.Roles
                        .Where(r => dto.RoleIds.Contains(r.Id))
                        .CountAsync(cancellationToken);

                    if (validRoles != dto.RoleIds.Count)
                        throw new InvalidOperationException("Một hoặc nhiều Role không tồn tại");

                    // Remove old roles
                    _dbContext.UserRoles.RemoveRange(user.UserRoles);

                    // Add new roles
                    var newUserRoles = dto.RoleIds.Select(roleId => new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow,
                    }).ToList();

                    _dbContext.UserRoles.AddRange(newUserRoles);
                    changes.Add($"Roles updated: {dto.RoleIds.Count} roles assigned");
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private void ValidateUserInput(UpdateUserDto dto)
        {
            var errors = new List<string>();

            // Email validation (nếu có)
            if (!string.IsNullOrWhiteSpace(dto.Email) && !IsValidEmail(dto.Email))
                errors.Add("Email không hợp lệ");

            // Username validation (nếu có)
            if (!string.IsNullOrWhiteSpace(dto.Fullname))
            {
                errors.Add("Fullname không được để trống");
            }

            // Phone validation (nếu có)
            if (!string.IsNullOrWhiteSpace(dto.Phone) && !IsValidPhone(dto.Phone))
                errors.Add("Số điện thoại không hợp lệ");

            // Fullname validation (nếu có)
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
        private bool IsValidPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^0\d{9,10}$");
        }
    }
}