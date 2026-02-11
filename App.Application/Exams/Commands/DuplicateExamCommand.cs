using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Exams.Commands
{
    // ============================================
    // UC-XX.4: DUPLICATE ĐỀ THI (Nhân bản)
    // ============================================
    public record DuplicateExamCommand : IRequest<Guid>
    {
        public Guid SourceExamId { get; set; }
        public string? NewCode { get; set; }
        public string? NewTitle { get; set; }
    }

    public class DuplicateExamHandler : IRequestHandler<DuplicateExamCommand, Guid>
    {
        private readonly IAppDbContext _context;

        public DuplicateExamHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(DuplicateExamCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.SourceExamId == Guid.Empty)
                throw new ValidationException("SourceExamId không hợp lệ");

            // === GET SOURCE EXAM WITH FULL DATA ===
            var sourceExam = await _context.Exams
                .Include(e => e.Sections)
                    .ThenInclude(s => s.ExamQuestions)
                .FirstOrDefaultAsync(x => x.Id == request.SourceExamId, cancellationToken);

            if (sourceExam == null)
                throw new Exception("Đề thi nguồn không tồn tại");

            // === GENERATE NEW CODE/TITLE ===
            var newCode = !string.IsNullOrWhiteSpace(request.NewCode)
                ? request.NewCode
                : $"{sourceExam.Code}-Copy-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var newTitle = !string.IsNullOrWhiteSpace(request.NewTitle)
                ? request.NewTitle
                : $"{sourceExam.Title} (Copy)";

            // Check duplicate code
            var codeExists = await _context.Exams
                .AnyAsync(e => e.Code == newCode && e.IsActive, cancellationToken);

            if (codeExists)
                throw new ValidationException($"Mã đề thi '{newCode}' đã tồn tại");

            // === CREATE NEW EXAM ===
            var newExam = new Exam
            {
                Id = Guid.NewGuid(),
                Code = newCode,
                Title = newTitle,
                Duration = sourceExam.Duration,
                TotalScore = sourceExam.TotalScore,
                Status = ExamStatus.Draft, // Luôn tạo ở trạng thái Draft
                Type = sourceExam.Type,
                ShuffleQuestions = sourceExam.ShuffleQuestions,
                ShuffleAnswers = sourceExam.ShuffleAnswers,
                Version = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Exams.Add(newExam);

            // === COPY SECTIONS & QUESTIONS ===
            foreach (var sourceSection in sourceExam.Sections.OrderBy(s => s.OrderIndex))
            {
                var newSection = new ExamSection
                {
                    Id = Guid.NewGuid(),
                    ExamId = newExam.Id,
                    CategoryId = sourceSection.CategoryId,
                    Instructions = sourceSection.Instructions,
                    OrderIndex = sourceSection.OrderIndex,
                    TimeLimit = sourceSection.TimeLimit,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ExamSections.Add(newSection);

                // Copy questions
                foreach (var sourceQuestion in sourceSection.ExamQuestions.OrderBy(q => q.OrderIndex))
                {
                    var newExamQuestion = new ExamQuestion
                    {
                        Id = Guid.NewGuid(),
                        ExamId = newExam.Id,
                        ExamSectionId = newSection.Id,
                        QuestionId = sourceQuestion.QuestionId, // Giữ nguyên QuestionId
                        QuestionNo = sourceQuestion.QuestionNo,
                        Point = sourceQuestion.Point,
                        OrderIndex = sourceQuestion.OrderIndex,
                        IsMandatory = sourceQuestion.IsMandatory,
                        IsShuffleable = sourceQuestion.IsShuffleable,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ExamQuestions.Add(newExamQuestion);
                }
            }

            // === SAVE ===
            await _context.SaveChangesAsync(cancellationToken);

            return newExam.Id;
        }
    }
}
