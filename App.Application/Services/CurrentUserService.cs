using App.Application.Interfaces;
using App.Application.Services.Interface;
using Microsoft.AspNetCore.Http;


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
        public Guid? UserId =>
        Guid.TryParse(
            _httpContextAccessor.HttpContext?.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            out var id)
            ? id
            : null;
    }
}
