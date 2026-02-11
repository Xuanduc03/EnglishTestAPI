using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Fullname { get; set; }
    }
    // auth for login info
    public class UserAuthInfoDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new();
    }
    public class LoginResultDto
    {
        public Guid UserId { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiredAt { get; set; }
    }

    public class LogoutRequest
    {
        public string RefreshToken { get; set; }
    }
}
