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
    // COMMAND 4: PATCH - Xóa 1 rule khỏi bảng quy đổi
    // DELETE /api/score-tables/{id}/rules/{correctAnswers}
    // ============================================================
    public class DeleteConversionRuleCommand : IRequest<bool>
    {
        public Guid ScoreTableId { get; set; }
        public int CorrectAnswers { get; set; }
    }

    public class DeleteConversionRuleCommandHandler
        : IRequestHandler<DeleteConversionRuleCommand, bool>
    {
        private readonly IAppDbContext _context;

        public DeleteConversionRuleCommandHandler(IAppDbContext context)
            => _context = context;

        public async Task<bool> Handle(
            DeleteConversionRuleCommand request,
            CancellationToken ct)
        {
            var entity = await _context.ScoreTables
                .FirstOrDefaultAsync(s => s.Id == request.ScoreTableId && !s.IsDeleted, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy ScoreTable");

            var rules = string.IsNullOrWhiteSpace(entity.ConversionJson)
                ? new Dictionary<string, int>()
                : JsonSerializer.Deserialize<Dictionary<string, int>>(entity.ConversionJson)
                  ?? new();

            var key = request.CorrectAnswers.ToString();
            if (!rules.ContainsKey(key))
                throw new KeyNotFoundException(
                    $"Không tìm thấy rule với số câu đúng = {request.CorrectAnswers}");

            rules.Remove(key);

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
