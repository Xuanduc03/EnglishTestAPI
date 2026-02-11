using App.Application.DTOs;
using Microsoft.AspNetCore.Http;


namespace App.Application.Interfaces
{
    public interface ICloudinaryService
    {
        // ================== FROM HTTP (FE upload) ==================
        Task<MediaUploadResult> UploadImageAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default);

        Task<MediaUploadResult> UploadAudioAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default);


        // ================== FROM FILE SYSTEM (ZIP import) ==================
        Task<MediaUploadResult> UploadImageAsync(
            string filePath,
            string folder,
            CancellationToken ct);

        Task<MediaUploadResult> UploadAudioAsync(
            string filePath,
            string folder,
            CancellationToken ct);


        // ================== LOW-LEVEL INTERNAL ==================
        Task<MediaUploadResult> UploadImageInternalAsync(
            Stream stream,
            string fileName,
            string folder,
            CancellationToken ct);

        Task<MediaUploadResult> UploadAudioInternalAsync(
            Stream stream,
            string fileName,
            string folder,
            CancellationToken ct);


        // ================== DELETE ==================
        Task DeleteAsync(
            string publicId,
            CancellationToken ct = default);
    }

}
