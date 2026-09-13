using App.Application.Auth.DTO;

namespace App.Application.Auth
{
    public interface IAuthService
    {
        Task<DTO.AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<DTO.AuthResult> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default);
        Task<DTO.AuthResult> GoogleLoginAsync(GoogleLoginRequest request, string? ip, string? userAgent, CancellationToken ct = default);
        Task<DTO.AuthResult> RefreshTokenAsync(string refreshToken, string? ip, string? userAgent, CancellationToken ct = default);
        Task LogoutAsync(string refreshToken, CancellationToken ct = default);
        Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
        Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    }
}
