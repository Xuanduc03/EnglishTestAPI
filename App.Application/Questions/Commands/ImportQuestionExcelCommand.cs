using App.Application.Interfaces;
using App.Application.Questions.Dtos;
using App.Application.Questions.Services;
using App.Application.Questions.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace App.Application.Questions.Commands
{
    // ======= COMMAND =======
    public record ImportQuestionExcelCommand : IRequest<ImportQuestionExcelResult>
    {
        public IFormFile File { get; init; }
    }

    // ======= DTOs =======
    public class ImportQuestionItemDto
    {
        public string SheetName { get; set; }
        public bool IsGroup { get; set; }
        public QuestionGroupImportDto Group { get; set; }
        public QuestionImportDto Question { get; set; }
    }

    public class QuestionImportDto
    {
        public Guid CategoryId { get; set; }
        public string Content { get; set; }
        public Guid? DifficultyId { get; set; }
        public string AudioUrl { get; set; }
        public string ImageUrl { get; set; }
        public List<AnswerImportDto> Answers { get; set; } = new();
        public string Explanation { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class QuestionGroupImportDto
    {
        public Guid CategoryId { get; set; }
        public string GroupTitle { get; set; }
        public string GroupContent { get; set; }
        public string AudioUrl { get; set; }
        public string ImageUrl { get; set; }
        public List<QuestionInGroupImportDto> Questions { get; set; } = new();
        public string Explanation { get; set; }
    }

    public class QuestionInGroupImportDto
    {
        public int QuestionNumber { get; set; }
        public string Content { get; set; }
        public Guid DifficultyId { get; set; }
        public List<AnswerImportDto> Answers { get; set; } = new();
        public string Explanation { get; set; }
    }

    public class AnswerImportDto
    {
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }

    public class AnswerLookUpDto
    {

        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
    }


    public class QuestionGroupsDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Content { get; set; }
    }

    public class QuestionLookUpDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Content { get; set; }
    }

    // ======= RESULT =======
    public class ImportQuestionExcelResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalImported { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalFailed { get; set; }
        public List<ImportFailedItemDto> FailedItems { get; set; } = new();
    }

    public class ImportFailedItemDto
    {
        public string SheetName { get; set; }
        public int? ItemIndex { get; set; }
        public string Reason { get; set; }
        public List<string> Details { get; set; } = new();
    }

    // ======= HANDLER =======
    public class ImportQuestionExcelCommandHandler
        : IRequestHandler<ImportQuestionExcelCommand, ImportQuestionExcelResult>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<ImportQuestionExcelCommandHandler> _logger;
        private readonly ICloudinaryService _cloudinary;
        private readonly IExcelZipParser _zipParser;
        private readonly IExcelZipImportService _zipImportService;

        // Config
        private readonly string[] AllowedAudioExt = new[] { ".mp3", ".wav", ".m4a" };
        private readonly string[] AllowedImageExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        public ImportQuestionExcelCommandHandler(
            IAppDbContext context,
            ILogger<ImportQuestionExcelCommandHandler> logger,
            ICloudinaryService cloudinary, IExcelZipParser zipParser,
        IExcelZipImportService zipImportService)
        {
            _context = context;
            _logger = logger;
            _cloudinary = cloudinary;
            _zipParser = zipParser;
            _zipImportService = zipImportService;
        }

        public async Task<ImportQuestionExcelResult> Handle(
            ImportQuestionExcelCommand request,
            CancellationToken cancellationToken)
        {


            if (request.File == null || request.File.Length == 0)
                throw new ArgumentException("File không được để trống");

            // ===== 4️⃣ LOAD DB DATA FOR VALIDATION =====

            // Load categories and difficulties
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.Name.Contains("Part"))
                .ToDictionaryAsync(c => c.Name, c => new CategoryLookupDto
                { Id = c.Id, Name = c.Name }, cancellationToken);

            var difficulties = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.CodeType == "Difficulty")
                .ToListAsync(cancellationToken);

            var existingQuestions = await _context.Questions
                 .AsNoTracking()
                 .Where(q => q.IsActive)
                 .OrderByDescending(q => q.CreatedAt)
                 .Take(1000)
                 .Select(q => new ExistingQuestionLite
                 {
                     Id = q.Id,
                     CategoryId = q.CategoryId,
                     Content = q.Content
                 })
                 .ToListAsync(cancellationToken);

            var existingAnswerSets = await _context.Questions
                 .Where(q => q.IsActive)
                 .Include(q => q.Answers)
                 .OrderByDescending(q => q.CreatedAt)
                 .Take(500)
                 .Select(q => new ExistingAnswerSetLite
                 {
                     Id = q.Id,
                     CategoryId = q.CategoryId,
                     Answers = q.Answers.OrderBy(a => a.OrderIndex).Select(a => a.Content).ToList()
                 })
                 .ToListAsync(cancellationToken);

            var existingGroups = await _context.QuestionGroups
                    .AsNoTracking()
                    .Where(g => g.IsActive)
                    .Select(g => new QuestionGroupsDto { Id = g.Id, CategoryId = g.CategoryId, Content = g.Content })
                    .ToListAsync(cancellationToken);


            // 2️⃣ Parse ZIP (GIỐNG preview)
            var parseContext = new ExcelZipParseContext
            {
                Categories = categories,
                ExistingQuestions = existingQuestions,
                ExistingAnswerSets = existingAnswerSets,
                ExistingGroups = existingGroups,
                ValidateAgainstDatabase = true
            };

            var parseResult = await _zipParser.ParseAsync(
               request.File,
               parseContext,
               cancellationToken);

            if (parseResult.HasError)
                throw new InvalidOperationException(
                    "File còn lỗi, không thể import");

            // 3️⃣ Import (CHỈ KHÁC PREVIEW Ở ĐÂY)
            return await _zipImportService.ImportAsync(
                parseResult,
                new ImportOptions(),
                cancellationToken);

        }
    }
}