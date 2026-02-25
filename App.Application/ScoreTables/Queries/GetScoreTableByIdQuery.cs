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
    // QUERY 2: Get by ID
    // GET /api/score-tables/{id}
    // ============================================================
    public record GetScoreTableByIdQuery(Guid Id) : IRequest<ScoreTableDto>;

    public class GetScoreTableByIdQueryHandler
        : IRequestHandler<GetScoreTableByIdQuery, ScoreTableDto>
    {
        private readonly IAppDbContext _context;

        public GetScoreTableByIdQueryHandler(IAppDbContext context)
            => _context = context;

        public async Task<ScoreTableDto> Handle(
            GetScoreTableByIdQuery request,
            CancellationToken ct)
        {
            var entity = await _context.ScoreTables
                .AsNoTracking()
                .Include(s => s.Exam)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy ScoreTable với Id = {request.Id}");

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
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                       ?? new();
            }
            catch { return new(); }
        }
    }

}
