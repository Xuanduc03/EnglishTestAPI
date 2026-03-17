using App.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace App.Application.ExamDigitize.Commands
{
    // ── Request ───────────────────────────────────────────────────
    public record UploadAndExtractCommand : IRequest<ExtractedExamDto>
    {
        public IFormFile File { get; init; } = null!;

        // IELTS_READING | IELTS_LISTENING | TOEIC_READING | TOEIC_LISTENING
        public string ExamType { get; init; } = "IELTS_READING";
    }

    // ── Result DTOs ───────────────────────────────────────────────
    public class ExtractedExamDto
    {
        public string ExamType { get; set; } = string.Empty;

        // Reading
        public string? PassageTitle { get; set; }
        public string? PassageContent { get; set; }

        // Listening
        public string? SectionTitle { get; set; }
        public string? Instructions { get; set; }

        // TOEIC
        public int? PartNumber { get; set; }

        public List<ExtractedQuestionDto> Questions { get; set; } = [];
    }

    public class ExtractedQuestionDto
    {
        public int OrderIndex { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int QuestionType { get; set; } = 1;
        public bool IsAiGraded { get; set; } = false;
        public string? SampleAnswer { get; set; }
        public int? MaxWords { get; set; }
        public List<ExtractedAnswerDto> Answers { get; set; } = [];
    }

    public class ExtractedAnswerDto
    {
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
        public int OrderIndex { get; set; }
    }

    // ── Handler ───────────────────────────────────────────────────
    public class UploadAndExtractCommandHandler
        : IRequestHandler<UploadAndExtractCommand, ExtractedExamDto>
    {
        private readonly IGeminiService _gemini;

        public UploadAndExtractCommandHandler(IGeminiService gemini)
        {
            _gemini = gemini;
        }

        public async Task<ExtractedExamDto> Handle(
            UploadAndExtractCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Base64
            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms, cancellationToken);
            var base64 = Convert.ToBase64String(ms.ToArray());

            // 2. Gọi Gemini
            var rawJson = await _gemini.ExtractExamAsync(
                base64,
                request.File.ContentType,
                request.ExamType,
                cancellationToken);

            // 3. Deserialize → DTO
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<ExtractedExamDto>(rawJson, options)
                ?? throw new InvalidOperationException("Không thể parse JSON từ AI");

            result.ExamType = request.ExamType;
            return result;
        }
    }
}