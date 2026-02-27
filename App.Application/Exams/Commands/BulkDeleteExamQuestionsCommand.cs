using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Exams.Commands
{
    public class BulkDeleteExamQuestionsCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public List<Guid> ExamQuestionIds { get; set; } = new();
    }

    public class BulkDeleteExamQuestionsCommandHandler : IRequestHandler<BulkDeleteExamQuestionsCommand, bool>
    {
        private readonly IAppDbContext _context;

        public BulkDeleteExamQuestionsCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(BulkDeleteExamQuestionsCommand request, CancellationToken cancellationToken)
        {
            // Lấy các ExamQuestion cần xóa
            var itemsToDelete = await _context.ExamQuestions
                .Where(eq => request.ExamQuestionIds.Contains(eq.Id) && eq.ExamId == request.ExamId)
                .ToListAsync(cancellationToken);

            if (!itemsToDelete.Any())
                return false;

            // Xóa
            _context.ExamQuestions.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
