using App.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace App.Application.ExamAttempts;

public static class GuestAttemptAccess
{
    public static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static void EnsureOwner(ExamAttempt attempt, Guid? userId, string? guestToken)
    {
        if (attempt.StudentId.HasValue)
        {
            if (userId.HasValue && attempt.Student?.UserId == userId.Value)
                return;
        }
        else if (!string.IsNullOrWhiteSpace(attempt.GuestTokenHash) &&
                 !string.IsNullOrWhiteSpace(guestToken) && guestToken.Length <= 128)
        {
            var expected = Convert.FromHexString(attempt.GuestTokenHash);
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(guestToken));
            if (CryptographicOperations.FixedTimeEquals(expected, actual))
                return;
        }

        throw new UnauthorizedAccessException("Không có quyền truy cập phiên thi này");
    }
}
