using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace App.Application.Exams.Commands
{
    // ============================================================
    // COMMAND: PUBLISH
    // POST /api/exams/{examId}/publish
    // Draft → Published (hoặc Suspended → Published)
    // ============================================================
    public record PublishExamCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
    }

    public class PublishExamHandler : IRequestHandler<PublishExamCommand, bool>
    {
        private readonly IAppDbContext _context;

        public PublishExamHandler(IAppDbContext context)
            => _context = context;

        public async Task<bool> Handle(
            PublishExamCommand request,
            CancellationToken cancellationToken)
        {
            var exam = await LoadAndValidateAsync(request.ExamId, cancellationToken);

            // === BUSINESS RULES: Chỉ cho publish từ Draft hoặc Suspended ===
            var allowedFrom = new[] { ExamStatus.Draft, ExamStatus.PendingReview, ExamStatus.Suspended };
            if (!allowedFrom.Contains(exam.Status))
                throw new InvalidOperationException(
                    $"Không thể xuất bản đề thi đang ở trạng thái '{exam.Status}'. " +
                    $"Chỉ chấp nhận: Draft, PendingReview, Suspended");

            // === VALIDATE ĐỦ ĐIỀU KIỆN PUBLISH ===
            await ValidateReadyToPublishAsync(exam, cancellationToken);

            exam.Status = ExamStatus.Published;
            exam.Version++;
            exam.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <summary>
        /// Kiểm tra đề thi đủ điều kiện publish
        /// - Có ít nhất 1 section
        /// - Mỗi section có ít nhất 1 câu hỏi
        /// </summary>
        private async Task ValidateReadyToPublishAsync(Exam exam, CancellationToken ct)
        {
            var sections = await _context.ExamSections
                .Where(s => s.ExamId == exam.Id && !s.IsDeleted)
                .Include(s => s.ExamQuestions)
                .ToListAsync(ct);

            if (!sections.Any())
                throw new ValidationException("Đề thi phải có ít nhất 1 section trước khi xuất bản");

            var emptySections = sections.Where(s => !s.ExamQuestions.Any()).ToList();
            if (emptySections.Any())
                throw new ValidationException(
                    $"Các section chưa có câu hỏi: {string.Join(", ", emptySections.Select(s => s.OrderIndex))}");

            if (exam.Category == ExamCategory.FullTest
                && exam.Type == ExamType.TOEIC
                && exam.Scope == ExamScope.Full)
            {
                var totalQuestions = sections
                    .SelectMany(s => s.ExamQuestions)
                    .Count();

                if (totalQuestions != 200)
                    throw new ValidationException(
                        $"TOEIC Full Test phải có đúng 200 câu. Hiện tại có {totalQuestions} câu.");

                ValidateToeicPartStructure(sections);
            }
        }

        private async Task<Exam> LoadAndValidateAsync(Guid examId, CancellationToken ct)
        {
            if (examId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == examId && x.IsActive, ct)
                ?? throw new KeyNotFoundException("Đề thi không tồn tại hoặc đã bị xóa");

            return exam;
        }


        private void ValidateToeicPartStructure(List<ExamSection> sections)
        {
            var expected = new Dictionary<string, int>
                {
                    { "Part 1", 6 },
                    { "Part 2", 25 },
                    { "Part 3", 39 },
                    { "Part 4", 30 },
                    { "Part 5", 30 },
                    { "Part 6", 16 },
                    { "Part 7", 54 }
                };

            // 1️⃣ Check đủ 7 part
            if (sections.Count != 7)
                throw new ValidationException("TOEIC Full Test phải có đủ 7 Part.");

            foreach (var part in expected)
            {
                var section = sections
                    .FirstOrDefault(s => s.Category.Name == part.Key);

                if (section == null)
                    throw new ValidationException($"Thiếu {part.Key}");

                var questionCount = section.ExamQuestions.Count;

                if (questionCount != part.Value)
                    throw new ValidationException(
                        $"{part.Key} phải có {part.Value} câu. Hiện tại có {questionCount} câu.");
            }

            var total = sections.SelectMany(s => s.ExamQuestions).Count();

            if (total != 200)
                throw new ValidationException(
                    $"Tổng số câu phải là 200. Hiện tại có {total} câu.");
        }
    }
}
