using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Auth.Commands
{
    public class RefreshTokenResultDto
    {
        public string accessToken { get; set;}
        public string refreshToken { get; set;}
        public DateTime expiredAt { get; set;}
    }
    public record RefreshTokenCommand(string refreshToken) : IRequest<RefreshTokenResultDto>;
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResultDto>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IConfiguration _config;

        // token expiry
        private const int TOKEN_EXPIRY_HOURS = 1;
        private const int REFRESH_TOKEN_DAYS = 7;

        public RefreshTokenCommandHandler(IAppDbContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _config = config;
        }

        public async Task<RefreshTokenResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var existingRefreshToken = await _dbContext.RefreshTokens
                 .Include(rt => rt.User)
                 .FirstOrDefaultAsync(rt => rt.Token == request.refreshToken, cancellationToken);

           // 2. validate 
           if(existingRefreshToken == null)
            {
                throw new UnauthorizedAccessException("Refresh token không hợp lệ");
            }

           if(existingRefreshToken.ExpiredAt < DateTime.UtcNow) { 
                // token hết hạn -> bắt đăng nhập lại -> xóa token rác
                _dbContext.RefreshTokens.Remove(existingRefreshToken);
                await _dbContext.SaveChangesAsync();
                throw new UnauthorizedAccessException("Token hết hạn vui lòng đăng nhập lại");
            }

            var user = existingRefreshToken.User;
            if (user == null) throw new UnauthorizedAccessException("Người dùng không tồn tại.");

            // 3. Generate New Access Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Fullname)
            };

            var roles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync(cancellationToken);

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(TOKEN_EXPIRY_HOURS),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Audience = _config["Jwt:Audience"],
                Issuer = _config["Jwt:Issuer"]
            };

            var newAccessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

            // 4. Rotate Refresh Token (Quan trọng)
            // Cách 1: Xóa cũ, tạo mới (Đơn giản nhất, tránh rác DB)
            _dbContext.RefreshTokens.Remove(existingRefreshToken);

            // Tạo chuỗi ngẫu nhiên
            var newRefreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = newRefreshTokenString,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(REFRESH_TOKEN_DAYS),
                // IsRevoked = false
            };

            _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 5. Return
            return new RefreshTokenResultDto
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshTokenString,
                expiredAt = DateTime.UtcNow.AddHours(TOKEN_EXPIRY_HOURS) // Trả về exp của AccessToken để FE biết
            };
        }
    }
}
