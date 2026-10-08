using App.Application.Interfaces;
using App.Application.Services.Interface;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace App.Application.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Lấy ID từ Claim (không tốn tài nguyên DB)
        public Guid? UserId
        {
            get
            {
                var principal = _httpContextAccessor.HttpContext?.User;
                var value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal?.FindFirst("sub")?.Value;
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }
    }
}
