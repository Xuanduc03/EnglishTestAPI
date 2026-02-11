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
    // Command : cập nhật điểm số câu hỏi
    public class UpdateQuestionPointCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public Guid ExamQuestionId { get; set; }
        public decimal NewPoint { get; set; }
    }

    public class UpdateQuestionPointHandler : IRequestHandler<UpdateQuestionPointCommand, bool>
    {
        private readonly IAppDbContext _context;

        public UpdateQuestionPointHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateQuestionPointCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            if (request.ExamQuestionId == Guid.Empty)
                throw new ValidationException("ExamQuestionId không hợp lệ");

            if (request.NewPoint <= 0)
                throw new ValidationException("Điểm số phải lớn hơn 0");

            // === GET EXAM QUESTION ===
            var examQuestion = await _context.ExamQuestions
                .FirstOrDefaultAsync(x => x.Id == request.ExamQuestionId, cancellationToken);

            if (examQuestion == null)
                throw new Exception("ExamQuestion không tồn tại");

            if (examQuestion.ExamId != request.ExamId)
                throw new Exception("Câu hỏi không thuộc đề thi này");

            // === CHECK EXAM STATUS ===
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == examQuestion.ExamId, cancellationToken);

            if (exam == null)
                throw new Exception("Exam không tồn tại");

            if (exam.Status != ExamStatus.Draft)
                throw new Exception("Chỉ được sửa đề thi ở trạng thái Draft");

            // === UPDATE POINT ===
            examQuestion.Point = request.NewPoint;

            // === CẬP NHẬT TOTAL SCORE ===
            var newTotalScore = await _context.ExamQuestions
                .Where(eq => eq.ExamId == request.ExamId)
                .SumAsync(eq => eq.Point, cancellationToken);

            exam.TotalScore = newTotalScore;

            // === SAVE ===
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

