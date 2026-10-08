using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace App.Application.Auth.Commands;

public record LoginUserCommand(string Email, string Password) : IRequest<LoginResultDto>;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResultDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IConfiguration _config;

    public LoginUserCommandHandler(IAppDbContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _config = config;
    }

    public async Task<LoginResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user == null)
        {
            await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");
        }
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa");
        if (user.IsLockedOut())
            throw new UnauthorizedAccessException("Tài khoản đang bị khóa tạm thời");

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.RecordLoginFailure(5, TimeSpan.FromMinutes(5));
            await _dbContext.SaveChangesAsync(cancellationToken);
            await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");
        }

        var jwtKey = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            throw new InvalidOperationException("Jwt ít nhất 32 ký tự");

        var now = DateTime.UtcNow;
        var accessExpiry = now.AddHours(1);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: accessExpiry,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256));

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = now.AddDays(7)
        });
        user.RecordLoginSuccess(now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResultDto
        {
            UserId = user.Id,
            Fullname = user.FullName,
            Email = user.Email,
            Roles = new List<string> { user.Role.ToString() },
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken,
            ExpiredAt = accessExpiry
        };
    }
}
