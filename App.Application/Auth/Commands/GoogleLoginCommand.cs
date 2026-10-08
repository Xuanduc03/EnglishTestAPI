using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Identity;
using Google.Apis.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace App.Application.Auth.Commands;

public record GoogleLoginCommand(string IdToken) : IRequest<LoginResultDto>;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResultDto>
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _config;

    public GoogleLoginCommandHandler(IAppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<LoginResultDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var clientId = _config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Google ClientId chưa được cấu hình");
        var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
            new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
        var email = payload.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user == null)
        {
            user = new User(email, payload.Name ?? email.Split('@')[0], UserRole.Student);
            user.StudentProfile = new Student { UserId = user.Id, Fullname = user.FullName, MemberLevel = MemberLevel.Standard, AvatarUrl = payload.Picture };
            _context.Users.Add(user);
        }
        if (user.IsDeleted || !user.IsActive || user.IsLockedOut())
            throw new UnauthorizedAccessException("Tài khoản đang bị khóa");

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
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))),
            ExpiresAt = now.AddDays(7)
        });
        user.RecordLoginSuccess(now);
        await _context.SaveChangesAsync(cancellationToken);

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
