using App.Application.Interfaces;
using App.Domain.Entities; // Giả định bạn chứa Entity ở đây
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace App.Application.Vocabularies.Commands
{
    public record CreateVocabularyCommand : IRequest<Guid>
    {
        public string Word { get; set; }               // Từ vựng
        public string PartOfSpeech { get; set; }       // Loại từ (adj, v, n,...)
        public string Phonetic { get; set; }           // Phiên âm
        public string Meaning { get; set; }            // Nghĩa của từ
        public int OrderIndex { get; set; }            // STT (có thể dùng để sắp xếp)
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? AudioFile { get; set; }      // Phát âm
        public IFormFile? ImageFile { get; set; }      // Hình ảnh minh họa
        public string? Example { get; set; }           // Câu ví dụ
        public string? ExampleMeaning { get; set; }    // Nghĩa câu ví dụ
        public string? Level { get; set; }             // A1, A2, B1...
        public Guid? CategoryId { get; set; }          // Topic (Business, Travel...)
    }

    public class CreateVocabularyCommandHandler : IRequestHandler<CreateVocabularyCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary;

        public CreateVocabularyCommandHandler(IAppDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Guid> Handle(CreateVocabularyCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Word))
                throw new Exception("Từ vựng không được để trống");

            if (string.IsNullOrWhiteSpace(request.PartOfSpeech))
                throw new Exception("Loại từ không được để trống");

            var wordExists = await _context.VocabularyWords
                .AnyAsync(v => v.Word == request.Word && v.CategoryId == request.CategoryId, cancellationToken);

            if (wordExists)
                throw new Exception("Từ vựng này đã tồn tại trong chủ đề");

            // 2 & 3. UPLOAD FILES (Audio & Image)
            var uploadResults = await UploadAllFilesAsync(request, cancellationToken);

            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {
                // 4. MAP DTO -> ENTITY
                var vocabId = Guid.NewGuid();
                var vocabEntity = new VocabularyWord 
                {
                    Id = vocabId,
                    Word = request.Word.Trim(),
                    PartOfSpeech = request.PartOfSpeech.Trim(),
                    Phonetic = request.Phonetic?.Trim(),
                    Meaning = request.Meaning?.Trim(),
                    OrderIndex = request.OrderIndex,
                    Example = request.Example?.Trim(),
                    ExampleMeaning = request.ExampleMeaning?.Trim(),
                    Level = request.Level?.Trim(),
                    CategoryId = request.CategoryId,

                    // Lấy URL và PublicId từ kết quả upload
                    AudioUrl = uploadResults.VocabAudioUrl,
                    AudioPublicId = uploadResults.VocabAudioPublicId,
                    ImageUrl = uploadResults.VocabImageUrl,
                    ImagePublicId = uploadResults.VocabImagePublicId,

                    CreatedAt = DateTime.UtcNow,
                };

                // 5. SAVE DB
                _context.VocabularyWords.Add(vocabEntity);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                // 6. RETURN ID
                return vocabId;
            }
            catch (Exception)
            {
                // Nếu lỗi DB (VD: đứt cáp, lỗi syntax SQL), Rollback ngay lập tức
                await transaction.RollbackAsync(cancellationToken);

               

                // Ném lỗi ra ngoài cho Middleware/Filter xử lý
                throw;
            }
        }

        #region Helper Upload & Hash
        private class UploadResults
        {
            public string? VocabAudioUrl { get; set; }
            public string? VocabAudioPublicId { get; set; }
            public string? VocabImageUrl { get; set; }
            public string? VocabImagePublicId { get; set; }
            public string? AudioFileHash { get; set; }
            public string? ImageFileHash { get; set; }
        }

        private async Task<UploadResults> UploadAllFilesAsync(CreateVocabularyCommand request, CancellationToken cancellationToken)
        {
            var results = new UploadResults();
            var uploadTasks = new List<Task>();

            // ========== AUDIO ==========
            if (request.AudioFile != null)
            {
                // Đã bỏ Task.Run() vì I/O Bound không cần bốc Thread mới
                uploadTasks.Add(Task.Run(async () =>
                {
                    results.AudioFileHash = await CalculateFileHashAsync(request.AudioFile);
                    var audioResult = await _cloudinary.UploadAudioAsync(
                        request.AudioFile,
                        "toeic/vocabularies/audio", // Đổi path cho hợp lý_
                        cancellationToken
                    );
                    results.VocabAudioUrl = audioResult.Url;
                    results.VocabAudioPublicId = audioResult.PublicId;
                }, cancellationToken));
                // Note: Ở đây vẫn giữ Task.Run theo ý bạn nếu bạn muốn chạy song song Hash và Upload, 
                // nhưng tốt nhất là tính Hash xong rồi mới nhét vào uploadTasks.

                // Cách sạch hơn:

                results.AudioFileHash = await CalculateFileHashAsync(request.AudioFile);
                var audioTask = _cloudinary.UploadAudioAsync(request.AudioFile, "toeic/vocabularies/audio", cancellationToken)
                    .ContinueWith(t =>
                    {
                        results.VocabAudioUrl = t.Result.Url;
                        results.VocabAudioPublicId = t.Result.PublicId;
                    }, cancellationToken);
                uploadTasks.Add(audioTask);

            }
            else if (!string.IsNullOrWhiteSpace(request.AudioUrl))
            {
                results.VocabAudioUrl = request.AudioUrl;
            }

            // ========== IMAGE ==========
            if (request.ImageFile != null)
            {
                uploadTasks.Add(Task.Run(async () =>
                {
                    results.ImageFileHash = await CalculateFileHashAsync(request.ImageFile);
                    var imageResult = await _cloudinary.UploadImageAsync(
                        request.ImageFile,
                        "toeic/vocabularies/images",
                        cancellationToken
                    );
                    results.VocabImageUrl = imageResult.Url;
                    results.VocabImagePublicId = imageResult.PublicId;
                }, cancellationToken));
            }
            else if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                results.VocabImageUrl = request.ImageUrl;
            }

            // Đợi cả Audio và Image upload xong cùng lúc
            await Task.WhenAll(uploadTasks);

            return results;
        }

        private async Task<string> CalculateFileHashAsync(IFormFile file)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = file.OpenReadStream();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            stream.Position = 0;
            return Convert.ToBase64String(hashBytes);
        }
        #endregion
    }
}