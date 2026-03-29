using App.Application.Interfaces;
using App.Application.Questions.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Queries
{
    public class ExportQuestionsQuery : IRequest<ExportFileDto>
    {
        public Guid? CategoryId { get; set; }
    }
    public class ExportFileDto
    {
        public byte[] Content { get; set; } = default!;   // dữ liệu file (Excel)
        public string FileName { get; set; } = default!;  // tên file
    }
    public class ExportQuestionsHandler
    : IRequestHandler<ExportQuestionsQuery, ExportFileDto>
    {
        private readonly IAppDbContext _context;
        private readonly IExcelService _excelService;

        public ExportQuestionsHandler(
            IAppDbContext context,
            IExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        public async Task<ExportFileDto> Handle(
            ExportQuestionsQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Groups
            var groups = await _context.QuestionGroups
                .AsNoTracking()
                .Select(g => new GroupExportRawDto
                {
                    Id = g.Id,
                    Content = g.Content,
                    Transcript = g.Transcript,
                    Explanation = g.Explanation,
                    Media = g.Media.Select(m => new MediaExportDto
                    {
                        OwnerType = "Group",
                        OwnerId = g.Id,
                        Url = m.Url,
                        Type = m.MediaType,
                        OrderIndex = m.OrderIndex
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            // 2. Questions
            var questions = await _context.Questions
                .AsNoTracking()
                .Select(q => new QuestionExportRawDto
                {
                    Id = q.Id,
                    GroupId = q.GroupId,
                    Content = q.Content,
                    Type = q.QuestionType.ToString(),
                    OrderIndex = q.OrderIndex,
                    Explanation = q.Explanation,
                    MetadataJson = q.MetadataJson,

                    Answers = q.Answers.Select(a => new AnswerExportDto
                    {
                        QuestionId = q.Id,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect,
                        OrderIndex = a.OrderIndex
                    }).ToList(),

                    Media = q.Media.Select(m => new MediaExportDto
                    {
                        OwnerType = "Question",
                        OwnerId = q.Id,
                        Url = m.Url,
                        Type = m.MediaType,
                        OrderIndex = m.OrderIndex
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            // 3. Flatten
            var answers = questions.SelectMany(x => x.Answers).ToList();
            var media = questions.SelectMany(x => x.Media)
                .Concat(groups.SelectMany(g => g.Media))
                .ToList();

            // 4. Generate Excel
            var fileBytes = _excelService.GenerateExcel(
                groups,
                questions,
                answers,
                media
            );

            return new ExportFileDto
            {
                Content = fileBytes,
                FileName = $"questions_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };
        }
    }
}
