using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
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
    // COMMAND 1: CREATE
    // POST /api/score-tables
    // ============================================================
    public class CreateScoreTableCommand : IRequest<Guid>
    {
        [Required]
        public Guid ExamId { get; set; }

        /// <summary>"Listening" hoặc "Reading"</summary>
        [Required]
        public Guid CategoryId { get; set; }

        /// <summary>Danh sách rules: [{correctAnswers: 10, score: 50}, ...]</summary>
        [Required, MinLength(1, ErrorMessage = "Phải có ít nhất 1 rule quy đổi")]
        public List<ConversionRuleDto> ConversionRules { get; set; } = new();
    }

    public class CreateScoreTableCommandHandler
        : IRequestHandler<CreateScoreTableCommand, Guid>
    {
        private readonly IAppDbContext _context;

        public CreateScoreTableCommandHandler(IAppDbContext context)
            => _context = context;

        public async Task<Guid> Handle(
            CreateScoreTableCommand request,
            CancellationToken ct)
        {
            // === VALIDATE ===
            await ValidateAsync(request, ct);

            // === BUILD ENTITY ===
            var conversionJson = BuildConversionJson(request.ConversionRules);

            var entity = new ScoreTable
            {
                Id = Guid.NewGuid(),
                ExamId = request.ExamId,
                CategoryId = request.CategoryId,
                ConversionJson = conversionJson,
            };

            _context.ScoreTables.Add(entity);
            await _context.SaveChangesAsync(ct);

            return entity.Id;
        }

        private async Task ValidateAsync(CreateScoreTableCommand request, CancellationToken ct)
        {
            // Check Exam exists
            var examExists = await _context.Exams
                .AnyAsync(e => e.Id == request.ExamId, ct);
            if (!examExists)
                throw new KeyNotFoundException($"Không tìm thấy Exam với Id = {request.ExamId}");

            if (request.CategoryId == Guid.Empty)
                throw new ArgumentException("CategoryId không được rỗng");

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == request.CategoryId, ct);

            if (!categoryExists)
                throw new KeyNotFoundException(
                    $"Không tìm thấy Category với Id = {request.CategoryId}");

            // Check duplicate (ExamId + SkillType là unique)
            var isDuplicate = await _context.ScoreTables
                .AnyAsync(s =>
                    s.ExamId == request.ExamId &&
                    s.CategoryId == request.CategoryId &&
                    !s.IsDeleted, ct);
            if (isDuplicate)
                throw new InvalidOperationException(
                    $"Bảng điểm cho Exam này với kỹ năng '{request.CategoryId}' đã tồn tại");

            // Check rules không trùng CorrectAnswers
            var duplicateKeys = request.ConversionRules
                .GroupBy(r => r.CorrectAnswers)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateKeys.Any())
                throw new ValidationException(
                    $"Số câu đúng bị trùng: {string.Join(", ", duplicateKeys)}");
        }

        private static string BuildConversionJson(List<ConversionRuleDto> rules)
        {
            // Sort theo key để dễ đọc
            var dict = rules
                .OrderBy(r => r.CorrectAnswers)
                .ToDictionary(r => r.CorrectAnswers.ToString(), r => r.Score);

            return JsonSerializer.Serialize(dict);
        }
    }
}
