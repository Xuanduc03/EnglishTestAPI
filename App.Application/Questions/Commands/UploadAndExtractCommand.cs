using App.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace App.Application.ExamDigitize.Commands
{
    // ── Request ───────────────────────────────────────────────────
    public record UploadAndExtractCommand : IRequest<ExtractedExamDto>
    {
        public List<IFormFile> Files { get; init; } = [];
        public string ExamType { get; init; } = "IELTS_READING";
    }

    // ── Result DTOs ───────────────────────────────────────────────
    public class ExtractedExamDto
    {
        public string ExamType { get; set; } = string.Empty;
        public string? PassageTitle { get; set; }
        public string? PassageContent { get; set; }
        public string? SectionTitle { get; set; }
        public string? Instructions { get; set; }
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
            if (!request.Files.Any())
                throw new ArgumentException("Chưa có file ảnh nào");

            // Chuyển tất cả file → base64
            var imageDataList = new List<(string Base64, string MimeType)>();
            foreach (var file in request.Files)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken);
                imageDataList.Add((
                    Convert.ToBase64String(ms.ToArray()),
                    file.ContentType
                ));
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            string rawJson;

            if (imageDataList.Count == 1)
            {
                // 1 ảnh → gọi method đơn
                rawJson = await _gemini.ExtractExamAsync(
                    imageDataList[0].Base64,
                    imageDataList[0].MimeType,
                    request.ExamType,
                    cancellationToken);
            }
            else
            {
                // Nhiều ảnh → gửi tất cả trong 1 request để AI merge
                rawJson = await _gemini.ExtractExamMultipleAsync(
                    imageDataList,
                    request.ExamType,
                    cancellationToken);
            }

            var result = JsonSerializer.Deserialize<ExtractedExamDto>(rawJson, options)
                ?? throw new InvalidOperationException("Không thể parse JSON từ AI");

            result.ExamType = request.ExamType;
            return result;
        }

        // ── Merge nhiều kết quả từ nhiều ảnh ─────────────────────
        private static ExtractedExamDto MergeResults(
            List<ExtractedExamDto> dtos,
            string examType)
        {
            if (dtos.Count == 0)
                throw new InvalidOperationException("Không có kết quả nào từ AI");

            if (dtos.Count == 1) return dtos[0];

            // Ảnh đầu tiên thường chứa passage/header
            var first = dtos[0];

            var merged = new ExtractedExamDto
            {
                ExamType = examType,
                PassageTitle = first.PassageTitle,
                SectionTitle = first.SectionTitle,
                Instructions = first.Instructions,
                PartNumber = first.PartNumber,

                // Merge passage: nối các đoạn văn từ các ảnh
                PassageContent = string.Join("\n\n", dtos
                    .Where(d => !string.IsNullOrWhiteSpace(d.PassageContent))
                    .Select(d => d.PassageContent)),

                // Merge questions: gom tất cả câu hỏi, re-index orderIndex
                Questions = dtos
                    .SelectMany(d => d.Questions)
                    .OrderBy(q => q.OrderIndex)
                    .Select((q, idx) => new ExtractedQuestionDto
                    {
                        OrderIndex = idx + 1,       // re-index 1, 2, 3...
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        IsAiGraded = q.IsAiGraded,
                        SampleAnswer = q.SampleAnswer,
                        MaxWords = q.MaxWords,
                        Answers = q.Answers
                            .OrderBy(a => a.OrderIndex)
                            .ToList(),
                    })
                    .ToList(),
            };

            return merged;
        }
    }
}