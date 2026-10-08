using App.Application.Interfaces;
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

public class RefreshTokenResultDto
{
    public string accessToken { get; set; } = default!;
    public string refreshToken { get; set; } = default!;
    public DateTime expiredAt { get; set; }
}

public record RefreshTokenCommand(string refreshToken) : IRequest<RefreshTokenResultDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResultDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IConfiguration _config;

    public RefreshTokenCommandHandler(IAppDbContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _config = config;
    }

    public async Task<RefreshTokenResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.refreshToken))
            throw new UnauthorizedAccessException("Refresh token không hợp lệ");

        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.refreshToken)));
        var oldToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (oldToken == null || !oldToken.IsActive || !oldToken.User.IsActive)
            throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn");

        var jwtKey = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            throw new InvalidOperationException("Jwt ít nhất 32 ký tự");

        var user = oldToken.User;
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
        var nextToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var nextHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(nextToken)));

        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);
        oldToken.Revoke(now);
        oldToken.ReplacedByTokenHash = nextHash;
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = nextHash,
            ExpiresAt = now.AddDays(7)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RefreshTokenResultDto
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            refreshToken = nextToken,
            expiredAt = accessExpiry
        };
    }
}
