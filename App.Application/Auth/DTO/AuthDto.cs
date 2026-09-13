namespace App.Application.Auth.DTO
{
    public record RegisterRequest(string Email, string Password, string FullName);
    public record LoginRequest(string Email, string Password);
    public record GoogleLoginRequest(string IdToken);
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Token, string NewPassword);

    public record AuthResult(
        Guid UserId, string Email, string FullName, string Role,
        string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
}
