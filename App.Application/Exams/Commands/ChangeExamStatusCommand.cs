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
    // ============================================================
    // COMMAND: CHANGE STATUS (tổng quát cho mọi transition)
    // PATCH /api/exams/{examId}/status
    // ============================================================
    public record ChangeExamStatusCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public ExamStatus NewStatus { get; set; }
        public string? Reason { get; set; } // Lý do (bắt buộc khi Suspend/Archive)
    }

    public class ChangeExamStatusHandler : IRequestHandler<ChangeExamStatusCommand, bool>
    {
        private readonly IAppDbContext _context;

        public ChangeExamStatusHandler(IAppDbContext context)
            => _context = context;

        public async Task<bool> Handle(
            ChangeExamStatusCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == request.ExamId && x.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Đề thi không tồn tại hoặc đã bị xóa");

            // === VALIDATE TRANSITION HỢP LỆ ===
            ValidateTransition(exam.Status, request.NewStatus, request.Reason);

            exam.Status = request.NewStatus;
            exam.Version++;
            exam.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <summary>
        /// State machine: Các chuyển trạng thái hợp lệ
        ///
        ///  Draft ──────────────────→ PendingReview
        ///  Draft ──────────────────→ Published
        ///  PendingReview ──────────→ Published
        ///  PendingReview ──────────→ Draft        (trả về sửa)
        ///  Published ──────────────→ Suspended
        ///  Published ──────────────→ Archived
        ///  Suspended ──────────────→ Published    (mở lại)
        ///  Suspended ──────────────→ Archived
        ///  Archived ───────────────→ Draft        (restore)
        /// </summary>
        private static readonly Dictionary<ExamStatus, ExamStatus[]> AllowedTransitions = new()
        {
            [ExamStatus.Draft] = new[] { ExamStatus.PendingReview, ExamStatus.Published },
            [ExamStatus.PendingReview] = new[] { ExamStatus.Draft, ExamStatus.Published },
            [ExamStatus.Published] = new[] { ExamStatus.Suspended, ExamStatus.Archived },
            [ExamStatus.Suspended] = new[] { ExamStatus.Published, ExamStatus.Archived },
            [ExamStatus.Archived] = new[] { ExamStatus.Draft },
        };

        private static void ValidateTransition(
            ExamStatus current,
            ExamStatus next,
            string? reason)
        {
            if (!AllowedTransitions.TryGetValue(current, out var allowed) ||
                !allowed.Contains(next))
            {
                throw new InvalidOperationException(
                    $"Không thể chuyển từ '{current}' sang '{next}'. " +
                    $"Cho phép: {string.Join(", ", AllowedTransitions[current])}");
            }

            // Bắt buộc nhập lý do khi Suspend / Archive
            if (next is ExamStatus.Suspended or ExamStatus.Archived
                && string.IsNullOrWhiteSpace(reason))
            {
                throw new ValidationException(
                    $"Vui lòng nhập lý do khi chuyển sang trạng thái '{next}'");
            }
        }
    }
}
