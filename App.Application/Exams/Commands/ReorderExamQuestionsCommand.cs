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
    // sắp xếp lại câu hỏi sau khi add  vào đề thi
    public class ReorderExamQuestionsCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public Guid SectionId { get; set; }
        public List<QuestionOrderItem> Items { get; set; }
    }

    public class QuestionOrderItem
    {
        public Guid ExamQuestionId { get; set; }
        public int OrderIndex { get; set; }
    }

    public class ReorderExamQuestionsHandler : IRequestHandler<ReorderExamQuestionsCommand, bool>
    {
        private readonly IAppDbContext _context;

        public ReorderExamQuestionsHandler(IAppDbContext examRepo)
        {
            _context = examRepo;
        }

        public async Task<bool> Handle(ReorderExamQuestionsCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            if (request.SectionId == Guid.Empty)
                throw new ValidationException("SectionId không hợp lệ");

            if (request.Items == null || !request.Items.Any())
                throw new ValidationException("Danh sách câu hỏi không được rỗng");

            // Check duplicate OrderIndex
            var orderIndexes = request.Items.Select(x => x.OrderIndex).ToList();
            if (orderIndexes.Distinct().Count() != orderIndexes.Count)
                throw new ValidationException("Thứ tự câu hỏi bị trùng");

            // === CHECK EXAM ===
            var exam = await _context.ExamSections.AnyAsync(x => x.ExamId == request.ExamId);
            if (exam == null)
                throw new Exception("Exam Không tồn tại");


            // === GET EXAM QUESTIONS ===
            var examQuestionIds = request.Items.Select(i => i.ExamQuestionId).ToList();

            var examQuestions = await _context.ExamQuestions
                .Where(eq => examQuestionIds.Contains(eq.Id))
                .ToListAsync(cancellationToken);

            if (examQuestions.Count != request.Items.Count)
                throw new Exception("Một số câu hỏi không tồn tại");

            // Check all belong to same section
            if (examQuestions.Any(eq => eq.ExamSectionId != request.SectionId))
                throw new Exception("Có câu hỏi không thuộc phần thi này");

            // === UPDATE ORDER ===
            foreach (var item in request.Items)
            {
                var examQuestion = examQuestions.First(eq => eq.Id == item.ExamQuestionId);
                examQuestion.OrderIndex = item.OrderIndex;
                examQuestion.QuestionNo = item.OrderIndex;
            }

            // === SAVE CHANGES ===
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

}
