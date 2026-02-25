using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.Application.ScoreTables.Queries
{
    // ============================================================
    // QUERY 3: Get by ExamId + SkillType (lookup nhanh)
    // GET /api/score-tables/by-exam/{examId}/{skillType}
    // ============================================================
    public record GetScoreTableByExamQuery(Guid ExamId, Guid CategoryId)
        : IRequest<ScoreTableDto?>;

    public class GetScoreTableByExamQueryHandler
        : IRequestHandler<GetScoreTableByExamQuery, ScoreTableDto?>
    {
        private readonly IAppDbContext _context;

        public GetScoreTableByExamQueryHandler(IAppDbContext context)
            => _context = context;

        public async Task<ScoreTableDto?> Handle(
            GetScoreTableByExamQuery request,
            CancellationToken ct)
        {
            var entity = await _context.ScoreTables
                .AsNoTracking()
                .Include(s => s.Exam)
                .FirstOrDefaultAsync(s =>
                    s.ExamId == request.ExamId &&
                    s.CategoryId == request.CategoryId &&
                    !s.IsDeleted, ct);

            if (entity == null) return null;

            return new ScoreTableDto
            {
                Id = entity.Id,
                ExamId = entity.ExamId,
                ExamTitle = entity.Exam?.Title ?? "",
                CategoryId = entity.CategoryId,
                ConversionRules = ParseConversionJson(entity.ConversionJson),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static Dictionary<string, int> ParseConversionJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new(); }
            catch { return new(); }
        }
    }
}

