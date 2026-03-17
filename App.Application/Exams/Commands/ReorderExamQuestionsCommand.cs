using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.Exams.Commands
{
    public class ReorderExamQuestionsCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public Guid SectionId { get; set; }
        public List<QuestionOrderItem> Items { get; set; } = new();
    }

    public class QuestionOrderItem
    {
        public Guid ExamQuestionId { get; set; }
        public int OrderIndex { get; set; }
    }

    public class ReorderExamQuestionsHandler : IRequestHandler<ReorderExamQuestionsCommand, bool>
    {
        private readonly IAppDbContext _context;

        public ReorderExamQuestionsHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ReorderExamQuestionsCommand request, CancellationToken cancellationToken)
        {
            // Validation
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");
            if (request.SectionId == Guid.Empty)
                throw new ValidationException("SectionId không hợp lệ");
            if (request.Items == null || !request.Items.Any())
                throw new ValidationException("Danh sách câu hỏi không được rỗng");

            var orderIndexes = request.Items.Select(x => x.OrderIndex).ToList();
            if (orderIndexes.Distinct().Count() != orderIndexes.Count)
                throw new ValidationException("Thứ tự câu hỏi bị trùng");

            // FIX 1: AnyAsync trả bool, không thể so sánh với null
            // Trước: var exam = await _context.ExamSections.AnyAsync(...) → bool
            //        if (exam == null) → luôn false, không bao giờ throw
            var sectionExists = await _context.ExamSections
                .AnyAsync(x => x.ExamId == request.ExamId && x.Id == request.SectionId, cancellationToken);
            if (!sectionExists)
                throw new ValidationException("Section không tồn tại hoặc không thuộc Exam này");

            // Load questions
            var examQuestionIds = request.Items.Select(i => i.ExamQuestionId).ToList();
            var examQuestions = await _context.ExamQuestions
                .Where(eq => examQuestionIds.Contains(eq.Id) && !eq.IsDeleted)
                .ToListAsync(cancellationToken);

            if (examQuestions.Count != request.Items.Count)
                throw new ValidationException("Một số câu hỏi không tồn tại hoặc đã bị xóa");

            if (examQuestions.Any(eq => eq.ExamSectionId != request.SectionId))
                throw new ValidationException("Có câu hỏi không thuộc phần thi này");

   
            foreach (var item in request.Items)
            {
                var eq = examQuestions.First(q => q.Id == item.ExamQuestionId);
                eq.OrderIndex = item.OrderIndex;
                eq.QuestionNo = item.OrderIndex + 1; // FIX 2: 1-based
                eq.UpdatedAt = DateTime.UtcNow;     // FIX 3: track update time
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}