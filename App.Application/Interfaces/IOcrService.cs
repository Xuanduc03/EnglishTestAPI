namespace App.Application.Interfaces
{
    public interface IOcrService
    {
        Task<string> ExtractTextAsync(byte[] imageBytes, CancellationToken ct = default);
        Task<string> ExtractTextMultipleAsync(List<byte[]> images, CancellationToken ct = default);
    }
}
