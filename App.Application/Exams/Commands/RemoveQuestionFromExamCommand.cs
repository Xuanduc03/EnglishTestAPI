using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Exams.Commands
{
    // Xóa đề thi khỏi đề
    public  record  RemoveQuestionFromExamCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public Guid ExamQuestionId { get; set; }
    }

    // Handler logic
    public class RemoveQuestionFromExamCommandHandler : IRequestHandler<RemoveQuestionFromExamCommand, bool>
    {
        private readonly IAppDbContext _context;

        public RemoveQuestionFromExamCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(RemoveQuestionFromExamCommand request, CancellationToken cancellationToken)
        {
            // === 1. LẤY EXAM QUESTION ===
            var examQuestion = await _context.ExamQuestions
                .FirstOrDefaultAsync(x => x.Id == request.ExamQuestionId, cancellationToken);

            if (examQuestion == null)
                throw new Exception("ExamQuestion không tồn tại");

            if (examQuestion.ExamId != request.ExamId)
                throw new Exception("Câu hỏi không thuộc đề thi này");

            // === 2. KIỂM TRA EXAM STATUS ===
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == examQuestion.ExamId, cancellationToken);

            if (exam == null)
                throw new Exception("Exam không tồn tại");

            if (exam.Status != ExamStatus.Draft)
                throw new Exception("Chỉ được sửa đề thi ở trạng thái Draft");

            // Lưu lại thông tin cần thiết
            var sectionId = examQuestion.ExamSectionId;
            var removedOrderIndex = examQuestion.OrderIndex;

            // === 3. XÓA EXAM QUESTION ===
            _context.ExamQuestions.Remove(examQuestion);

            // === 4. REORDER CÁC CÂU CÒN LẠI TRONG SECTION ===
            var remainingQuestions = await _context.ExamQuestions
                .Where(eq => eq.ExamSectionId == sectionId && eq.OrderIndex > removedOrderIndex)
                .ToListAsync(cancellationToken);

            foreach (var question in remainingQuestions)
            {
                question.OrderIndex--;
                question.QuestionNo--;
            }

            // === 5. CẬP NHẬT TOTAL SCORE CỦA EXAM ===
            var newTotalScore = await _context.ExamQuestions
                .Where(eq => eq.ExamId == request.ExamId && eq.Id != request.ExamQuestionId)
                .SumAsync(eq => eq.Point, cancellationToken);

            exam.TotalScore = newTotalScore;

            // === 6. LƯU THAY ĐỔI ===
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

}
