using App.Application.DTOs;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using CloudinarySdk = CloudinaryDotNet.Cloudinary;
using App.Application.Interfaces;

namespace App.Infrastructure.Cloudinary
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly CloudinarySdk _cloudinary;
        private static readonly string[] AllowedImageExt =
            [".jpg", ".jpeg", ".png", ".webp"];

        private static readonly string[] AllowedAudioExt =
            [".mp3", ".wav", ".ogg", ".m4a"];
        public CloudinaryService(IOptions<CloudinaryOptions> options)
        {
            var opt = options.Value;

            var account = new Account(
                opt.CloudName,
                opt.ApiKey,
                opt.ApiSecret
            );

            _cloudinary = new CloudinarySdk(account);
        }
        // ================= IFORMFILE =================

        public async Task<MediaUploadResult> UploadImageAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default)
        {
            ValidateImage(file);

            await using var stream = file.OpenReadStream();
            return await UploadImageInternalAsync(
                stream,
                file.FileName,
                folder,
                ct);
        }

        public async Task<MediaUploadResult> UploadAudioAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default)
        {
            ValidateAudio(file);

            await using var stream = file.OpenReadStream();
            return await UploadAudioInternalAsync(
                stream,
                file.FileName,
                folder,
                ct);
        }

        // ================= FILE PATH (ZIP / IMPORT) =================

        public async Task<MediaUploadResult> UploadImageAsync(
            string filePath,
            string folder,
            CancellationToken ct)
        {
            ValidateImage(filePath);

            await using var stream = File.OpenRead(filePath);
            return await UploadImageInternalAsync(
                stream,
                Path.GetFileName(filePath),
                folder,
                ct);
        }

        public async Task<MediaUploadResult> UploadAudioAsync(
            string filePath,
            string folder,
            CancellationToken ct)
        {
            ValidateAudio(filePath);

            await using var stream = File.OpenRead(filePath);
            return await UploadAudioInternalAsync(
                stream,
                Path.GetFileName(filePath),
                folder,
                ct);
        }

        // ================= CORE UPLOAD =================

        public async Task<MediaUploadResult> UploadImageInternalAsync(
            Stream stream,
            string fileName,
            string folder,
            CancellationToken ct)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation()
                    .Width(800)
                    .Height(600)
                    .Crop("limit")
                    .Quality("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams, ct);

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return new MediaUploadResult(
                result.SecureUrl.ToString(),
                result.PublicId);
        }

        public async Task<MediaUploadResult> UploadAudioInternalAsync(
            Stream stream,
            string fileName,
            string folder,
            CancellationToken ct)
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams, ct);

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return new MediaUploadResult(
                result.SecureUrl.ToString(),
                result.PublicId);
        }

        // ================= DELETE =================

        public async Task DeleteAsync(
            string publicId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return;

            var result = await _cloudinary.DestroyAsync(
                new DeletionParams(publicId));

            if (result.Result != "ok" && result.Result != "not found")
                throw new Exception($"Delete failed: {result.Result}");
        }

        // ================= VALIDATION =================
        private static void ValidateImage(IFormFile file)
        {
            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };

            if (file == null || file.Length == 0)
                throw new ArgumentException("Image file is empty");

            if (!allowed.Contains(file.ContentType.ToLower()))
                throw new ArgumentException("Invalid image type");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Image must be <= 5MB");
        }

        private static void ValidateAudio(IFormFile file)
        {
            var allowed = new[] { "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg" };

            if (file == null || file.Length == 0)
                throw new ArgumentException("Audio file is empty");

            if (!allowed.Contains(file.ContentType.ToLower()))
                throw new ArgumentException("Invalid audio type");

            if (file.Length > 10 * 1024 * 1024)
                throw new ArgumentException("Audio must be <= 10MB");
        }


        private static void ValidateImage(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (!AllowedImageExt.Contains(ext))
                throw new ArgumentException("Invalid image type");
        }

        private static void ValidateAudio(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (!AllowedAudioExt.Contains(ext))
                throw new ArgumentException("Invalid audio type");
        }
    }

}
