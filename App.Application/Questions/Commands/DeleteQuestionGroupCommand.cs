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
    // XÓA NHÓM CÂU HỎI (GROUP)
    public record DeleteQuestionGroupCommand(Guid Id, bool HardDelete = false) : IRequest<bool>;

    public class DeleteQuestionGroupCommandHandler : IRequestHandler<DeleteQuestionGroupCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<DeleteQuestionGroupCommandHandler> _logger;

        public DeleteQuestionGroupCommandHandler(IAppDbContext dbContext, ILogger<DeleteQuestionGroupCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteQuestionGroupCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

            try
            {
                var group = await _dbContext.QuestionGroups
                    .Include(g => g.Questions)
                        .ThenInclude(q => q.Answers)
                    .Include(g => g.Questions)
                        .ThenInclude(q => q.Media)
                    .Include(g => g.Media)
                    .FirstOrDefaultAsync(g => g.Id == request.Id && !g.IsDeleted, cancellationToken);
                if (group == null)
                    throw new KeyNotFoundException("Nhóm câu hỏi không tồn tại");

                // Check dependencies (e.g., group link exam/result)
                var hasExamQuestions = await _dbContext.ExamQuestions.AnyAsync(eq => group.Questions.Select(q => q.Id).Contains(eq.QuestionId), cancellationToken);
                if (hasExamQuestions)
                    throw new InvalidOperationException("Không thể xóa nhóm vì có câu hỏi liên kết với đề thi");

                if (request.HardDelete)
                {
                    // Xóa hard: Cascade remove questions/answers/media
                    foreach (var question in group.Questions)
                    {
                        _dbContext.Answers.RemoveRange(question.Answers);
                        _dbContext.QuestionMedias.RemoveRange(question.Media);
                        _dbContext.Questions.Remove(question);
                    }
                    _dbContext.QuestionGroupMedia.RemoveRange(group.Media);
                    _dbContext.QuestionGroups.Remove(group);
                }
                else
                {
                    // Soft delete group và cascade inactive questions
                    group.IsDeleted = true;
                    group.UpdatedAt = DateTime.UtcNow;
                    foreach (var question in group.Questions)
                    {
                        question.IsDeleted = true;
                        question.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Deleted question group {GroupId} (hard: {HardDelete})", request.Id, request.HardDelete);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error deleting question group {Id}", request.Id);
                throw;
            }
        }
    }
}
