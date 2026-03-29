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
        public string ExamType { get; init; } = "TOEIC_READING";
        public bool PassageOnly { get; init; } = false;
        public bool QuestionsOnly { get; init; } = false;
        public string? PassageContent { get; init; }
    }

    // ── DTOs ──────────────────────────────────────────────────────
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
        public string? Explanation { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
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
        private readonly IOcrService _ocr;

        public UploadAndExtractCommandHandler(IGeminiService gemini, IOcrService ocr)
        {
            _gemini = gemini;
            _ocr = ocr;
        }

        public async Task<ExtractedExamDto> Handle(
            UploadAndExtractCommand request,
            CancellationToken cancellationToken)
        {
            // FIX 2: đọc bytes 1 lần duy nhất — dùng lại cho cả OCR và Gemini
            var imageBytes = new List<byte[]>();
            var imageDataList = new List<(string Base64, string MimeType)>();

            foreach (var file in request.Files)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken);
                var bytes = ms.ToArray();
                imageBytes.Add(bytes);
                imageDataList.Add((Convert.ToBase64String(bytes), file.ContentType));
            }

            // ── PassageOnly: dùng Tesseract OCR 
            if (request.PassageOnly)
            {
                var passageText = await _ocr.ExtractTextMultipleAsync(imageBytes, cancellationToken);

                return new ExtractedExamDto
                {
                    ExamType = request.ExamType,
                    PassageTitle = ExtractTitle(passageText),
                    PassageContent = passageText,
                    Questions = [],
                };
            }

            // ── QuestionsOnly với passage context ─────────────────
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            string rawJson;

            if (request.QuestionsOnly && !string.IsNullOrWhiteSpace(request.PassageContent))
            {
                rawJson = await _gemini.ExtractQuestionsWithPassageAsync(
                    imageDataList,
                    request.PassageContent,
                    request.ExamType,
                    cancellationToken);
            }
            // ── Single image ──────────────────────────────────────
            else if (imageDataList.Count == 1)
            {
                var examTypeKey = request.QuestionsOnly
                    ? request.ExamType + "_QUESTIONS_ONLY"
                    : request.ExamType;

                rawJson = await _gemini.ExtractExamAsync(
                    imageDataList[0].Base64,
                    imageDataList[0].MimeType,
                    examTypeKey,
                    cancellationToken);
            }
            // ── Multiple images ───────────────────────────────────
            else
            {
                var examTypeKey = request.QuestionsOnly
                    ? request.ExamType + "_QUESTIONS_ONLY"
                    : request.ExamType;

                rawJson = await _gemini.ExtractExamMultipleAsync(
                    imageDataList, examTypeKey, cancellationToken);
            }

            var result = JsonSerializer.Deserialize<ExtractedExamDto>(rawJson, options)
                ?? throw new InvalidOperationException("Không thể parse JSON từ AI");

            result.ExamType = request.ExamType;
            return result;
        }

        // ── ExtractTitle: lấy dòng đầu làm title ─────────────────
        public static string? ExtractTitle(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var firstLine = text
                .Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 3);

            if (firstLine == null) return null;

            // Title thường là ALL CAPS
            return firstLine == firstLine.ToUpper() ? firstLine : null;
        }
    }
}