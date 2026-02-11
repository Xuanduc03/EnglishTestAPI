using App.Application.DTOs;
using App.Domain.Entities;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace App.Application.Auth.Commands
{

    public record LoginUserCommand(string Email, string Password) : IRequest<LoginResultDto>;

    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResultDto>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IConfiguration _config;

        // constants
        private const int MAX_FAILED_ATTEMPTS = 5;
        private const int LOCKOUT_MINUTES = 5;
        private const int TOKEN_EXPIRY_HOURS = 1;
        private const int REFRESH_TOKEN_DAYS = 7;

        public LoginUserCommandHandler(IAppDbContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _config = config;
        }


        public async Task<LoginResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
           try
            {
                // validate input
                if(string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    throw new UnauthorizedAccessException("Thông tin đăng nhập ko chính xác");
                }

                // get user 
                var user = await _dbContext.Users
                    .AsNoTracking()
                   .Where(u => u.Email.ToLower() == request.Email.ToLower())
                    .Select(u => new UserAuthInfoDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Fullname = u.Fullname,
                        PasswordHash = u.Password,
                        IsActive = u.IsActive,
                        FailedLoginAttempts = u.FailedLoginAttempts,
                        LockoutEnd = u.LockoutEnd,
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    // Delay để chống timing attack
                    await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);
                    throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");
                }

                // 4. Check account status
                if (!user.IsActive)
                {
                    throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa");
                }

                // Check khóa tài khoản (Sử dụng LockoutEnd)
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                {
                    var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                    throw new UnauthorizedAccessException($"Tài khoản bị khóa. Vui lòng thử lại sau {remainingMinutes} phút.");
                }

                // 6. Verify password
                bool isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

                if (!isValid)
                {
                    await HandleFailedLogin(user.Id, cancellationToken);
                    // Delay để chống timing attack
                    await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);
                    throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng");
                }

                // 7. Reset failed attempts on successful login
                await ResetFailedLoginAttempts(user.Id, cancellationToken);

                // 8. Generate tokens
                var accessToken = GenerateAccessToken(user);
                var refreshToken = GenerateRefreshToken();
                var expires = DateTime.UtcNow.AddHours(TOKEN_EXPIRY_HOURS);

                // 9. Save refresh token
                await SaveRefreshToken(user.Id, refreshToken, cancellationToken);

                // 10. Update last login
                var userEntity = await _dbContext.Users.FindAsync(new object[] { user.Id }, cancellationToken);
                if (userEntity != null)
                {
                    userEntity.LastLogin = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                return new LoginResultDto
                {
                    UserId = user.Id,
                    Fullname = user.Fullname,
                    Email = user.Email,
                    Roles = user.Roles,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    ExpiredAt = expires
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // method create jwt token
        public string GenerateAccessToken(UserAuthInfoDto user)
        {
            //validate JWT configuration
            var jwtKey = _config["Jwt:Key"];
            if(string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            {
                throw new InvalidOperationException("Jwt ít nhất 32 ký tự");
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
                new Claim(ClaimTypes.Name, user.Fullname)
            };

            // add roles 
            foreach(var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(TOKEN_EXPIRY_HOURS),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        } 

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task SaveRefreshToken(Guid userId, string refreshToken, CancellationToken cancellation)
        {
            var tokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(REFRESH_TOKEN_DAYS),
            };

            _dbContext.RefreshTokens.Add(tokenEntity);
            await _dbContext.SaveChangesAsync(cancellation);
        }

        private async Task HandleFailedLogin(Guid userId, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null) return;

            user.FailedLoginAttempts++;
            user.UpdatedAt = DateTime.UtcNow;

            // Lock account after max attempts
            if (user.FailedLoginAttempts >= MAX_FAILED_ATTEMPTS)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LOCKOUT_MINUTES);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task ResetFailedLoginAttempts(Guid userId, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null) return;

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

}

