using App.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Vocabularies.Commands
{
    public record UpdateVocabularyCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string Word { get; set; }
        public string PartOfSpeech { get; set; }
        public string Phonetic { get; set; }
        public string Meaning { get; set; }
        public int OrderIndex { get; set; }
        public IFormFile? AudioFile { get; set; }  // File âm thanh mới (nếu có)
        public IFormFile? ImageFile { get; set; }  // Hình ảnh mới (nếu có)
        public bool DeleteAudio { get; set; }      // Cờ đánh dấu xóa âm thanh hiện tại
        public bool DeleteImage { get; set; }      // Cờ đánh dấu xóa ảnh hiện tại
        public string? Example { get; set; }
        public string? ExampleMeaning { get; set; }
        public string? Level { get; set; }
        public Guid? CategoryId { get; set; }
    }

    public class UpdateVocabularyCommandHandler : IRequestHandler<UpdateVocabularyCommand, bool>
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary;

        public UpdateVocabularyCommandHandler(IAppDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<bool> Handle(UpdateVocabularyCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra sự tồn tại của từ vựng
            var vocab = await _context.VocabularyWords
                .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

            if (vocab == null) throw new Exception("Không tìm thấy từ vựng cần cập nhật");

            // 2. Kiểm tra trùng tên (nếu đổi Word sang từ khác đã có trong Topic)
            if (vocab.Word.ToLower() != request.Word.Trim().ToLower())
            {
                var wordExists = await _context.VocabularyWords
                    .AnyAsync(v => v.Word.ToLower() == request.Word.Trim().ToLower()
                                   && v.CategoryId == request.CategoryId, cancellationToken);
                if (wordExists) throw new Exception("Từ vựng này đã tồn tại trong chủ đề");
            }

            // 3. Xử lý Media (Audio/Image)
            // Nếu có file mới hoặc yêu cầu xóa -> Xóa file cũ trên Cloudinary trước
            if (request.AudioFile != null || request.DeleteAudio)
            {
                if (!string.IsNullOrEmpty(vocab.AudioPublicId))
                {
                    await _cloudinary.DeleteAsync(vocab.AudioPublicId); 
                    vocab.AudioUrl = null;
                    vocab.AudioPublicId = null;
                }
            }

            if (request.ImageFile != null || request.DeleteImage)
            {
                if (!string.IsNullOrEmpty(vocab.ImagePublicId))
                {
                    await _cloudinary.DeleteAsync(vocab.ImagePublicId);
                    vocab.ImageUrl = null;
                    vocab.ImagePublicId = null;
                }
            }

            // 4. Upload file mới nếu có
            if (request.AudioFile != null)
            {
                var audioResult = await _cloudinary.UploadAudioAsync(request.AudioFile, "toeic/vocabularies/audio", cancellationToken);
                vocab.AudioUrl = audioResult.Url;
                vocab.AudioPublicId = audioResult.PublicId;
            }

            if (request.ImageFile != null)
            {
                var imageResult = await _cloudinary.UploadImageAsync(request.ImageFile, "toeic/vocabularies/images", cancellationToken);
                vocab.ImageUrl = imageResult.Url;
                vocab.ImagePublicId = imageResult.PublicId;
            }

            // 5. Map lại các thông tin còn lại
            vocab.Word = request.Word.Trim();
            vocab.PartOfSpeech = request.PartOfSpeech.Trim();
            vocab.Phonetic = request.Phonetic?.Trim();
            vocab.Meaning = request.Meaning?.Trim();
            vocab.OrderIndex = request.OrderIndex;
            vocab.Example = request.Example?.Trim();
            vocab.ExampleMeaning = request.ExampleMeaning?.Trim();
            vocab.Level = request.Level?.Trim();
            vocab.CategoryId = request.CategoryId;
            vocab.UpdatedAt = DateTime.UtcNow; 

            _context.VocabularyWords.Update(vocab);
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}