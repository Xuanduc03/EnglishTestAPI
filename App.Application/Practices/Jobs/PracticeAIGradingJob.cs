using App.Application.Interfaces;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Practices.Jobs
{

    /// <summary>
    /// Background job: Chấm bài Writing/Speaking bằng AI sau khi student submit
    /// Được enqueue bởi SubmitPracticeCommandHandler
    /// </summary>
    public class PracticeAIGradingJob
    {
        private readonly IAppDbContext _context;
        private readonly IAIGradingService _aiGrading;
        private readonly ILogger<PracticeAIGradingJob> _logger;

        public PracticeAIGradingJob(
            IAppDbContext context,
            IAIGradingService aiGrading,
            ILogger<PracticeAIGradingJob> logger)
        {
            _context = context;
            _aiGrading = aiGrading;
            _logger = logger;
        }

        /// <summary>
        /// Chấm 1 PracticeAnswer cụ thể
        /// Hangfire sẽ retry tự động nếu thất bại (mặc định 10 lần)
        /// </summary>
        public async Task GradeAnswerAsync(Guid practiceAnswerId)
        {
            _logger.LogInformation("AI Grading started for PracticeAnswer {Id}", practiceAnswerId);

            // 1. Load answer + question
            var answer = await _context.PracticeAnswers
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == practiceAnswerId);

            if (answer == null)
            {
                _logger.LogWarning("PracticeAnswer {Id} not found", practiceAnswerId);
                return;
            }

            if (answer.GradingStatus == GradingStatusEnum.Completed)
            {
                _logger.LogInformation("Already graded, skipping {Id}", practiceAnswerId);
                return;
            }

            // 2. Đánh dấu đang chấm
            answer.GradingStatus = GradingStatusEnum.Grading;
            await _context.SaveChangesAsync();

            try
            {
                AIGradingResult result;
                var questionType = answer.Question.QuestionType;

                // 3. Gọi AI tương ứng
                if (questionType == QuestionTypeEnum.Writing
                    && !string.IsNullOrWhiteSpace(answer.TextAnswer))
                {
                    result = await _aiGrading.GradeWritingAsync(
                        questionContent: answer.Question.Content ?? "",
                        studentAnswer: answer.TextAnswer,
                        rubricJson: answer.Question.RubricJson,
                        promptType: QuestionPromptType.Writing);
                }
                else if (questionType == QuestionTypeEnum.Speaking
                    && !string.IsNullOrWhiteSpace(answer.AudioUrl))
                {
                    result = await _aiGrading.GradeSpeakingAsync(
                        questionContent: answer.Question.Content ?? "",
                        audioUrl: answer.AudioUrl,
                        rubricJson: answer.Question.RubricJson);
                }
                else
                {
                    _logger.LogWarning("Answer {Id} has no content to grade", practiceAnswerId);
                    answer.GradingStatus = GradingStatusEnum.Failed;
                    await _context.SaveChangesAsync();
                    return;
                }

                // 4. Lưu kết quả
                if (result.Success)
                {
                    answer.AiFeedback = result.Feedback;
                    answer.AiScoreDetailJson = result.ScoreDetailJson;
                    answer.IsAiGraded = true;
                    answer.GradedAt = DateTime.UtcNow;
                    answer.GradingStatus = GradingStatusEnum.Completed;

                    // Cập nhật IsCorrect dựa trên score (>= 5 = pass)
                    answer.IsCorrect = result.Score >= 5.0;

                    _logger.LogInformation(
                        "AI Grading completed for {Id}: Score={Score}",
                        practiceAnswerId, result.Score);
                }
                else
                {
                    answer.GradingStatus = GradingStatusEnum.Failed;
                    answer.AiFeedback = $"Chấm bài tự động thất bại: {result.ErrorMessage}";
                    _logger.LogError(
                        "AI Grading failed for {Id}: {Error}",
                        practiceAnswerId, result.ErrorMessage);
                }

                await _context.SaveChangesAsync();

                // 5. Cập nhật lại attempt summary nếu tất cả đã chấm xong
                await TryUpdateAttemptSummaryAsync(answer.PracticeAttemptId);
            }
            catch (Exception ex)
            {
                answer.GradingStatus = GradingStatusEnum.Failed;
                await _context.SaveChangesAsync();
                _logger.LogError(ex, "Unexpected error grading {Id}", practiceAnswerId);
                throw; // Hangfire sẽ retry
            }
        }

        /// <summary>
        /// Sau khi tất cả AI answers đã chấm xong -> cập nhật lại Score của attempt
        /// </summary>
        private async Task TryUpdateAttemptSummaryAsync(Guid attemptId)
        {
            var attempt = await _context.PracticeAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null) return;

            var aiAnswers = attempt.Answers
                .Where(a => a.GradingStatus != GradingStatusEnum.NotRequired)
                .ToList();

            // Chỉ update khi tất cả AI answers đã hoàn thành
            var allDone = aiAnswers.All(a =>
                a.GradingStatus == GradingStatusEnum.Completed
                || a.GradingStatus == GradingStatusEnum.Failed);

            if (!allDone) return;

            // Tính lại CorrectAnswers (bao gồm cả AI-graded)
            attempt.CorrectAnswers = attempt.Answers.Count(a => a.IsCorrect);
            attempt.Score = attempt.TotalQuestions > 0
                ? Math.Round((double)attempt.CorrectAnswers / attempt.TotalQuestions * 10, 2)
                : 0;
            attempt.AccuracyPercentage = attempt.TotalQuestions > 0
                ? Math.Round((double)attempt.CorrectAnswers / attempt.TotalQuestions * 100, 2)
                : 0;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Attempt {Id} summary updated after AI grading. Score={Score}",
                attemptId, attempt.Score);
        }
    }
}