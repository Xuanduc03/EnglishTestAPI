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
    // B2 : Thêm section (phần thi)
    // Input: ExamId, Name, Instructions, OrderIndex
    // API: POST /api/exams/{examId}/sections
    public class AddExamSectionCommand : IRequest<Guid>
    {
        public Guid ExamId { get; set; }
        public Guid CategoryId { get; set; }
        public string? Instructions { get; set; }
        public int OrderIndex { get; set; }
        public int? TimeLimit { get; set; }
    }

    public class AddExamSectionHandler : IRequestHandler<AddExamSectionCommand, Guid>
    {
        private readonly IAppDbContext _examRepo;

        public AddExamSectionHandler(IAppDbContext examRepo)
        {
            _examRepo = examRepo;
        }

        public async Task<Guid> Handle(AddExamSectionCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            if (request.CategoryId == Guid.Empty)
                throw new ValidationException("CategoryId không hợp lệ");

            if (request.OrderIndex < 0)
                throw new ValidationException("Thứ tự phần thi phải >= 0");

            if (request.TimeLimit.HasValue && request.TimeLimit.Value <= 0)
                throw new ValidationException("Thời gian phần thi phải lớn hơn 0");

            // === CHECK EXAM EXISTS ===
            var examExists = await _examRepo.Exams
                .AnyAsync(x => x.Id == request.ExamId, cancellationToken);

            if (!examExists)
                throw new Exception($"Không tìm thấy Exam");

            var categoryExists = await _examRepo.Categories
                .AnyAsync(x => x.Id == request.CategoryId);

            if (!categoryExists)
                throw new ValidationException("Category không tồn tại");


            // === CHECK CATEGORY ALREADY EXISTS IN EXAM ===
            var categoryAlreadyInExam = await _examRepo.ExamSections
                .AnyAsync(x =>
                    x.ExamId == request.ExamId &&
                    x.CategoryId == request.CategoryId &&
                    !x.IsDeleted,
                    cancellationToken);

            var exam = await _examRepo.Exams
                .Include(x => x.Sections)
                .FirstOrDefaultAsync(x => x.Id == request.ExamId, cancellationToken);

            if (exam == null)
                throw new Exception("Không tìm thấy Exam");

            var category = await _examRepo.Categories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId, cancellationToken);

            if (category == null)
                throw new ValidationException("Category không tồn tại");
            ValidateSectionAllowedByExamConfig(exam, category);

            if (categoryAlreadyInExam)
                throw new ValidationException("Category này đã tồn tại trong đề thi");

            // === CREATE SECTION ===
            var section = new ExamSection
            {
                Id = Guid.NewGuid(),
                ExamId = request.ExamId,
                CategoryId = request.CategoryId,
                Instructions = request.Instructions,
                OrderIndex = request.OrderIndex,
                TimeLimit = request.TimeLimit,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _examRepo.ExamSections.Add(section);

            await _examRepo.SaveChangesAsync(cancellationToken);
            return section.Id;
        }

        private void ValidateSectionAllowedByExamConfig(Exam exam, Category category)
        {
            if (exam.Type != ExamType.TOEIC)
                return;

            var partName = category.Name;

            if (exam.Scope == ExamScope.ListeningOnly)
            {
                var allowed = new[] { "Part 1", "Part 2", "Part 3", "Part 4" };

                if (!allowed.Contains(partName))
                    throw new ValidationException(
                        $"Đề ListeningOnly không được thêm {partName}");
            }

            if (exam.Scope == ExamScope.ReadingOnly)
            {
                var allowed = new[] { "Part 5", "Part 6", "Part 7" };

                if (!allowed.Contains(partName))
                    throw new ValidationException(
                        $"Đề ReadingOnly không được thêm {partName}");
            }

            if (exam.Scope == ExamScope.Full)
            {
                var allowed = new[]
                {
                    "Part 1","Part 2","Part 3","Part 4",
                    "Part 5","Part 6","Part 7"
                };

                if (!allowed.Contains(partName))
                    throw new ValidationException(
                        $"Full Test chỉ chấp nhận Part 1 → 7");
            }
        }
    }

}
