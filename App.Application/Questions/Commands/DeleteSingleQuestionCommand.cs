using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Commands
{
    // XÓA CÂU HỎI SINGLE
    public record DeleteQuestionCommand(Guid Id, bool HardDelete = false) : IRequest<bool>;

    public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ICloudinaryService _cloudinary;

        private readonly ILogger<DeleteQuestionCommandHandler> _logger;

        public DeleteQuestionCommandHandler(IAppDbContext dbContext, ILogger<DeleteQuestionCommandHandler> logger, ICloudinaryService cloudinary)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cloudinary = cloudinary;
        }

        public async Task<bool> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                var question = await _dbContext.Questions
                    .Include(q => q.Answers)
                    .Include(q => q.Media)
                    .FirstOrDefaultAsync(q => q.Id == request.Id && !q.IsDeleted, cancellationToken);
                if (question == null)
                    throw new KeyNotFoundException("Câu hỏi không tồn tại");

                // Check dependencies (business: không xóa nếu link exam/result)
                var hasExamQuestions = await _dbContext.ExamQuestions.AnyAsync(eq => eq.QuestionId == request.Id, cancellationToken);
                var hasStudentAnswers = await _dbContext.StudentAnswers.AnyAsync(sa => sa.QuestionId == request.Id, cancellationToken);
                if (hasExamQuestions || hasStudentAnswers)
                    throw new InvalidOperationException("Không thể xóa câu hỏi vì đã liên kết với đề thi hoặc kết quả");

                if (request.HardDelete)
                {
                    // 1️⃣ Xóa file cloudinary
                    foreach (var media in question.Media)
                    {
                        if (!string.IsNullOrWhiteSpace(media.PublicId))
                        {
                            await _cloudinary.DeleteAsync(media.PublicId, cancellationToken);
                        }
                    }

                    // 2️⃣ Xóa DB
                    _dbContext.Answers.RemoveRange(question.Answers);
                    _dbContext.QuestionMedias.RemoveRange(question.Media);
                    _dbContext.Questions.Remove(question);
                }
                else
                {
                    // Soft delete
                    question.IsDeleted = true;
                    question.UpdatedAt = DateTime.UtcNow;
                    // Optional: Inactive answers/media nếu cần
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Deleted question {QuestionId} (hard: {HardDelete})", request.Id, request.HardDelete);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error deleting question {Id}", request.Id);
                throw;
            }
        }
    }
}
