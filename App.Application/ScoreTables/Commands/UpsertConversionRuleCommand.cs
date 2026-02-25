using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.Application.ScoreTables.Commands
{
    // ============================================================
    // COMMAND 3: PATCH - Chỉ upsert 1 rule (thêm/sửa 1 entry)
    // PATCH /api/score-tables/{id}/rules
    // ============================================================
    public class UpsertConversionRuleCommand : IRequest<bool>
    {
        public Guid ScoreTableId { get; set; }

        /// <summary>Số câu đúng cần thêm/sửa</summary>
        public int CorrectAnswers { get; set; }

        /// <summary>Điểm mới</summary>
        public int Score { get; set; }
    }

    public class UpsertConversionRuleCommandHandler
        : IRequestHandler<UpsertConversionRuleCommand, bool>
    {
        private readonly IAppDbContext _context;

        public UpsertConversionRuleCommandHandler(IAppDbContext context)
            => _context = context;

        public async Task<bool> Handle(
            UpsertConversionRuleCommand request,
            CancellationToken ct)
        {
            var entity = await _context.ScoreTables
                .FirstOrDefaultAsync(s => s.Id == request.ScoreTableId && !s.IsDeleted, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy ScoreTable");

            // Parse JSON hiện tại
            var rules = string.IsNullOrWhiteSpace(entity.ConversionJson)
                ? new Dictionary<string, int>()
                : JsonSerializer.Deserialize<Dictionary<string, int>>(entity.ConversionJson)
                  ?? new();

            // Upsert (thêm mới hoặc override)
            rules[request.CorrectAnswers.ToString()] = request.Score;

            // Sort lại và save
            entity.ConversionJson = JsonSerializer.Serialize(
                rules.OrderBy(kv => int.Parse(kv.Key))
                     .ToDictionary(kv => kv.Key, kv => kv.Value)
            );
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }

}
