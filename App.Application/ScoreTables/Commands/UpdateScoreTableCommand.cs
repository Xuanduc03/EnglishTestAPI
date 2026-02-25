using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.Application.ScoreTables.Commands
{
    // ============================================================
    // COMMAND 2: UPDATE (thay toàn bộ)
    // PUT /api/score-tables/{id}
    // ============================================================
    public class UpdateScoreTableCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public List<ConversionRuleDto> ConversionRules { get; set; } = new();
    }

    public class UpdateScoreTableCommandHandler
        : IRequestHandler<UpdateScoreTableCommand, bool>
    {
        private readonly IAppDbContext _context;

        public UpdateScoreTableCommandHandler(IAppDbContext context)
            => _context = context;

        public async Task<bool> Handle(
            UpdateScoreTableCommand request,
            CancellationToken ct)
        {
            var entity = await _context.ScoreTables
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy ScoreTable với Id = {request.Id}");

            // Validate SkillType
            if (request.CategoryId == Guid.Empty)
                throw new ArgumentException("CategoryId không được rỗng");

            // Check duplicate nếu đổi SkillType
            if (entity.CategoryId != request.CategoryId)
            {
                var isDuplicate = await _context.ScoreTables
                    .AnyAsync(s =>
                        s.ExamId == entity.ExamId &&
                        s.CategoryId == request.CategoryId &&
                        s.Id != request.Id &&
                        !s.IsDeleted, ct);
                if (isDuplicate)
                    throw new InvalidOperationException(
                        $"Bảng điểm '{request.CategoryId}' cho Exam này đã tồn tại");
            }

            // Check duplicate rules
            var duplicateKeys = request.ConversionRules
                .GroupBy(r => r.CorrectAnswers)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateKeys.Any())
                throw new ValidationException(
                    $"Số câu đúng bị trùng: {string.Join(", ", duplicateKeys)}");

            // Update
            entity.CategoryId = request.CategoryId;
            entity.ConversionJson = JsonSerializer.Serialize(
                request.ConversionRules
                    .OrderBy(r => r.CorrectAnswers)
                    .ToDictionary(r => r.CorrectAnswers.ToString(), r => r.Score)
            );
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
