using App.Application.Interfaces;
using App.Application.Questions.Dtos;
using App.Application.Questions.Services;
using App.Application.Questions.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.RegularExpressions;

namespace App.Application.Questions.Commands
{
    #region Khai báo các lớp record cho preview question
    public record PreviewQuestionExcelCommand : IRequest<PreviewQuestionResult>
    {
        public IFormFile File { get; init; }
    }

    public record ImportAllPartsFromZipCommand : IRequest<ImportAllPartsResult>
    {
        // You can pass the same zip file or a previously stored tempId
        public IFormFile ZipFile { get; init; }
        public bool OverwriteExistingMedia { get; init; } = false;
    }

    public class ImportAllPartsResult
    {
        public string Message { get; set; }
        public int TotalImported { get; set; }
        public List<string> UploadedMediaUrls { get; set; } = new();
        public List<ImportFailureInfo> Failures { get; set; } = new();
    }

    public class ImportFailureInfo
    {
        public string Reason { get; set; }
        public string SheetName { get; set; }
        public int? Row { get; set; }
    }

    #endregion

    // ======= HANDLER (ONE COMMAND FOR ALL PARTS) =======
    public class PreviewAllPartsFromExcelCommandHandler
        : IRequestHandler<PreviewQuestionExcelCommand, PreviewQuestionResult>
    {
        private readonly IAppDbContext _context;

        private readonly IExcelZipParser _zipParser;

        public PreviewAllPartsFromExcelCommandHandler(IAppDbContext context, IExcelZipParser zipParser)
        {
            _context = context;
            _zipParser = zipParser;
        }

        public async Task<PreviewQuestionResult> Handle(
            PreviewQuestionExcelCommand request,
            CancellationToken cancellationToken)
        {


            // Load categories and difficulties
           

            var difficulties = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.CodeType == "Difficulty")
                .ToListAsync(cancellationToken);

            // Load recent existing questions/groups for similarity/duplicate checks
            var existingQuestions = await _context.Questions
                .Where(q => q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .Take(1000)
                .Select(q => new ExistingQuestionLite { Id = q.Id, CategoryId = q.CategoryId, Content = q.Content })
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
                .Include(g => g.Questions)
                    .ThenInclude(q => q.Answers)
                .Where(g => g.IsActive)
                .OrderByDescending(g => g.CreatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);


            var categories = await _context.Categories
             .AsNoTracking()
             .Where(c => c.IsActive && c.Name.Contains("Part"))
             .ToDictionaryAsync(c => c.Name, c => new CategoryLookupDto
             { Id = c.Id, Name = c.Name }, cancellationToken);

            var result = new PreviewQuestionResult
            {
                Message = "Preview toàn bộ Parts từ Excel thành công"
            };



            var parseContext = new ExcelZipParseContext
            {
                Categories = categories,
                ExistingQuestions = existingQuestions,
                ExistingAnswerSets = existingAnswerSets,
                ValidateAgainstDatabase = true
            };

            var parseResult = await _zipParser.ParseAsync(
                request.File,
                parseContext,
                cancellationToken);


            int totalProcessed = 0;

            foreach (var sheet in parseResult.Sheets)
            {
                foreach (var item in sheet.Items)
                {
                    switch (item)
                    {
                        case QuestionPreviewDto:
                            totalProcessed += 1;
                            break;

                        case QuestionGroupPreviewDto group:
                            totalProcessed += group.Questions?.Count ?? 0;
                            break;
                    }
                }
            }
            return new PreviewQuestionResult
            {
                Message = "Preview toàn bộ Parts từ Excel thành công",
                TotalProcessed = totalProcessed,
                Data = parseResult.Sheets,
                MissingMediaFiles = parseContext.MissingMediaFiles.ToList()
            };
        }
    }
}
