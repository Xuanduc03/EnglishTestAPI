using App.Domain.Entities;
using App.Application.Interfaces;
using Google.Apis.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using App.Application.DTOs;

namespace App.Application.Auth.Commands
{
    public record GoogleLoginCommand(string IdToken) : IRequest<LoginResultDto>;
    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResultDto>
    {
        private readonly IAppDbContext _context;
        private readonly IConfiguration _config;

        // constants
        private const int TOKEN_EXPIRY_HOURS = 1;
        private const int REFRESH_TOKEN_DAYS = 7;

        public static readonly Guid StudentId = Guid.Parse("d1812f11-590c-462e-b6d0-8bfa4f269bb2");
        public GoogleLoginCommandHandler(IAppDbContext context, IConfiguration config )
        {
            _context = context;
            _config = config;
        }
        public async Task<LoginResultDto> Handle(GoogleLoginCommand request, CancellationToken cancellation)
        {
            try
            {
                // 1. Xác thực idToken với Google
                var setting = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,setting);

                var email = payload.Email;

                // check user 
                var user = await _context.Users
                 .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                 .Include(u => u.StudentProfile) // Include luôn profile để check
                 .FirstOrDefaultAsync(u => u.Email == email, cancellation);

                if (user == null)
                {
                    user = CreateNewGoogleUser(payload);
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync(cancellation);
                }

                // 4. check lock
                if (!user.IsActive)
                    throw new UnauthorizedAccessException("Tài khoản bị khóa");

                if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
                    throw new UnauthorizedAccessException("Tài khoản đang bị khóa tạm thời");

                var authInfo = await _context.Users
                     .AsNoTracking()
                     .Where(u => u.Email == email)
                     .Select(u => new UserAuthInfoDto
                     {
                         Id = u.Id,
                         Email = u.Email,
                         Fullname = u.Fullname,
                         IsActive = u.IsActive,
                         LockoutEnd = u.LockoutEnd,

                         Roles = u.UserRoles
                             .Select(ur => ur.Role.Name)
                             .ToList(),
                     })
                     .FirstOrDefaultAsync(cancellation);
                
                // Tạo JWT
                var accessToken = GenerateAccessToken(authInfo);
                var refreshToken = GenerateRefreshToken();
                var expires = DateTime.UtcNow.AddHours(TOKEN_EXPIRY_HOURS);
                await SaveRefreshToken(authInfo.Id, refreshToken, cancellation); 

                return new LoginResultDto
                {
                    UserId = authInfo.Id,
                    Fullname = authInfo.Fullname,
                    Email = authInfo.Email,
                    Roles = authInfo.Roles,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    ExpiredAt = expires
                };
            }
            catch (Exception ex)
            {
                throw ;
            }
        }

        // create new user gg
        private User CreateNewGoogleUser(GoogleJsonWebSignature.Payload payload)
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = payload.Email,
                Fullname = payload.Name ?? payload.Email.Split('@')[0], 
                Password = null,
                EmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                AvatarUrl = payload.Picture 
            };

            user.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = StudentId 
            });

            // Tạo Student Profile luôn
            user.StudentProfile = new Student
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Fullname = user.Fullname,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            return user;
        }

        // function helper 
        // method create jwt token
        public string GenerateAccessToken(UserAuthInfoDto user)
        {
            //validate JWT configuration
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
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
            foreach (var role in user.Roles)
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

            _context.RefreshTokens.Add(tokenEntity);
            await _context.SaveChangesAsync(cancellation);
        }
    }
}
