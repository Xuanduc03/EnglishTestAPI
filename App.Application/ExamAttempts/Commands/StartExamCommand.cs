using App.Application.Interfaces;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.ExamAttempts.Commands
{
    // ============================================================
    // COMMAND: START EXAM
    // POST /api/exam-attempts/start
    // ============================================================
    public class StartExamCommand : IRequest<StartExamResult>
    {
        [Required]
        public Guid ExamId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class StartExamResult
    {
        public Guid AttemptId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int TotalQuestions { get; set; }

        // Danh sách câu hỏi (KHÔNG có đáp án đúng)
        public List<ExamQuestionPreview> Questions { get; set; } = new();
    }

    public class ExamQuestionPreview
    {
        public Guid ExamQuestionId { get; set; }      // ID trong bảng ExamQuestion
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public double Point { get; set; }
        public string Content { get; set; }
        public string QuestionType { get; set; }
        public List<AnswerOption> Answers { get; set; } = new();
        public bool HasAudio { get; set; }
        public bool HasImage { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
    }
    public class AnswerOption
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; }
        // ❌ KHÔNG GỬI IsCorrect cho FE khi đang thi!
    }
    public class StartExamCommandHandler : IRequestHandler<StartExamCommand, StartExamResult>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private const int MaxQuestionsPerExam = 500; // Safety limit
        private const int MaxDurationMinutes = 480; // 8 hours max

        // Constructor nhận dependencies từ DI container
        public StartExamCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            // Guard clauses: Kiểm tra null và throw ngay nếu DI inject null
            _context = context ?? throw new ArgumentNullException(nameof(context)); ;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<StartExamResult> Handle(
            StartExamCommand request,
            CancellationToken cancellationToken)
        {
            // 0. authorization
            Guid currentUserId;
            bool isGuest = false;
            var loggedInUserId = _currentUserService.UserId;

            if (request.UserId == Guid.Empty)
            {
                currentUserId = Guid.NewGuid();
                isGuest = true;
            }
            else
            {
                if (loggedInUserId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException(
                        "Người dùng chưa được xác thực. Vui lòng đăng nhập để sử dụng UserId được cung cấp.");
                }

                if (request.UserId != loggedInUserId)
                {
                    throw new UnauthorizedAccessException(
                        $"Người dùng đã đăng nhập ({loggedInUserId}) không được phép bắt đầu bài thi cho người dùng khác ({request.UserId}).");
                }
                currentUserId = request.UserId;
            }

            // 1. load & validate exam 
            var exam = await _context.Exams
               .AsNoTracking()
               .Where(e => e.Id == request.ExamId && !e.IsDeleted)
               .FirstOrDefaultAsync(cancellationToken)
               ?? throw new KeyNotFoundException(
                   $"Không tìm thấy bài thi {request.ExamId} hoặc bài thi đã bị xóa.");

            ValidateExamStatus(exam);

            // === 2. CHECK USER CAN START ===
            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            {
                try
                {
                    var activeAttempt = await _context.ExamAttempts
                        .FirstOrDefaultAsync(a =>
                            a.UserId == request.UserId &&
                            a.ExamId == request.ExamId &&
                            a.Status == ExamAttemptStatus.InProgress &&
                            a.ExpiresAt > DateTime.UtcNow,
                            cancellationToken)
                        ?? null;

                    if (activeAttempt != null)
                    {
                        throw new InvalidOperationException(
                                $"Đã tồn tại phiên thi đang làm: {activeAttempt.Id}. Vui lòng tiếp tục hoặc nộp bài trước khi bắt đầu phiên mới.");
                    }

                    // === 3. CREATE EXAM ATTEMPT ===
                    var now = DateTime.UtcNow;
                    var timeLimitSeconds = CalculateTimeLimitSeconds(exam.Duration);
                    var expiresAt = now.AddSeconds(timeLimitSeconds);

                    var attempt = new ExamAttempt
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        ExamId = exam.Id,
                        StartedAt = now,
                        ExpiresAt = expiresAt,
                        TimeLimitSeconds = timeLimitSeconds,
                        Status = ExamAttemptStatus.InProgress,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };

                    // === 4. LOAD QUESTIONS (Separate Query - Avoid Cartesian) ===
                    var allQuestions = await LoadQuestionsOptimized(
                        request.ExamId, cancellationToken);

                    if (!allQuestions.Any())
                        throw new InvalidOperationException(
                            "Bài thi không có câu hỏi để hiển thị");

                    if (allQuestions.Count > MaxQuestionsPerExam)
                        throw new InvalidOperationException(
                             $"Bài thi vượt quá giới hạn số câu hỏi cho phép: {allQuestions.Count}/{MaxQuestionsPerExam}.");

                    attempt.TotalQuestions = allQuestions.Count;

                    // === 5. CREATE EXAM ANSWERS ===
                    var examAnswers = allQuestions.Select(eq => new ExamAnswer
                    {
                        Id = Guid.NewGuid(),
                        ExamAttemptId = attempt.Id,
                        ExamQuestionId = eq.Id,
                        QuestionId = eq.QuestionId,
                        IsAnswered = false,
                        IsCorrect = false, 
                        Point = 0,
                        VersionNumber = 1,
                        CreatedAt = now,
                        UpdatedAt = now,
                    }).ToList();

                    // === 6. ANTI-CHEAT CHECK ===
                    //var antiCheatResult = await _antiCheatService.CheckStartExamAsync(
                    //    request.UserId, request.ExamId, request.IpAddress, cancellationToken);

                    //if (antiCheatResult.IsSuspicious)
                    //{
                    //    attempt.AntiCheatFlags.Add(antiCheatResult.Reason);
                    //} 

                    // === 7. SAVE TO DB ===
                    _context.ExamAttempts.Add(attempt);
                    _context.ExamAnswers.AddRange(examAnswers);
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    // === 8. BUILD RESPONSE ===
                    var result = BuildStartExamResult(attempt, allQuestions);
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }


        // ✅ HELPER: Validate Exam Status
        private void ValidateExamStatus(Exam exam)
        {
            if (exam.Status != ExamStatus.Published)
                throw new InvalidOperationException(
                    $"Trạng thái bài thi hiện tại là {exam.Status}, yêu cầu phải ở trạng thái Published.");

            var now = DateTime.UtcNow;
            if (exam.StartDate.HasValue && exam.StartDate > now)
                throw new InvalidOperationException(
                    $"Bài thi sẽ bắt đầu vào lúc {exam.StartDate}, hiện chưa đến thời gian mở thi.");

            if (exam.EndDate.HasValue && exam.EndDate < now)
                throw new InvalidOperationException(
                     $"Bài thi đã kết thúc vào lúc {exam.EndDate}, hiện không còn khả dụng.");

            if (exam.Duration <= 0 || exam.Duration > MaxDurationMinutes)
                throw new InvalidOperationException(
                    $"Thời lượng bài thi không hợp lệ: {exam.Duration} phút.");

            if (string.IsNullOrEmpty(exam.Code))
                throw new InvalidOperationException("Bài thi chưa được cấu hình mã đề (Exam Code).");
        }


        // ✅ HELPER: Calculate Time Limit (Validation)
        private int CalculateTimeLimitSeconds(int durationMinutes)
        {
            if (durationMinutes <= 0 || durationMinutes > MaxDurationMinutes)
                throw new ArgumentOutOfRangeException(
                    nameof(durationMinutes),
                    $"Thời lượng bài thi phải nằm trong khoảng từ 1 đến {MaxDurationMinutes} phút.");

            return durationMinutes * 60;
        }


        // ✅ HELPER: Load Questions Optimized (Avoid N+1)
        private async Task<List<ExamQuestionWithAnswers>> LoadQuestionsOptimized(
            Guid examId,
            CancellationToken cancellationToken)
        {
            // ✅ Use AsNoTracking + separate queries
            var sections = await _context.ExamSections
                .AsNoTracking()
                .Where(s => s.ExamId == examId && !s.IsDeleted)
                .OrderBy(s => s.OrderIndex)
                .Select(s => new { s.Id, s.OrderIndex })
                .ToListAsync(cancellationToken);

            var questions = await _context.ExamQuestions
                .AsNoTracking()
                .Where(eq => sections.Select(s => s.Id).Contains(eq.ExamSectionId))
                .Include(eq => eq.Question)
                    .ThenInclude(q => q.Answers.OrderBy(a => a.OrderIndex))
                .Include(eq => eq.Question)
                    .ThenInclude(q => q.Media)
                .OrderBy(eq => eq.OrderIndex)
                .ToListAsync(cancellationToken);

            return questions
                .OrderBy(q => sections.FirstOrDefault(s => s.Id == q.ExamSectionId)?.OrderIndex)
                .ThenBy(q => q.OrderIndex)
                .Select(eq => new ExamQuestionWithAnswers
                {
                    Id = eq.Id,
                    QuestionId = eq.QuestionId,
                    OrderIndex = eq.OrderIndex,
                    Point = (double)eq.Point,
                    Question = eq.Question,
                })
                .ToList();
        }


        // ✅ HELPER: Build Response (No Correct Answers)
        private StartExamResult BuildStartExamResult(
            ExamAttempt attempt,
            List<ExamQuestionWithAnswers> questions)
        {
            return new StartExamResult
            {
                AttemptId = attempt.Id,
                StartedAt = attempt.StartedAt,
                ExpiresAt = attempt?.ExpiresAt,
                TimeLimitSeconds = attempt.TimeLimitSeconds,
                TotalQuestions = attempt.TotalQuestions,
                Questions = questions.Select(eq => new ExamQuestionPreview
                {
                    ExamQuestionId = eq.Id,
                    QuestionId = eq.QuestionId,
                    OrderIndex = eq.OrderIndex,
                    Point = eq.Point,
                    Content = eq.Question.Content ?? string.Empty,
                    QuestionType = eq.Question.QuestionType,
                    HasAudio = eq.Question.Media?.Any(m => m.MediaType == "audio") ?? false,
                    HasImage = eq.Question.Media?.Any(m => m.MediaType == "image") ?? false,
                    AudioUrl = eq.Question.Media?
                        .FirstOrDefault(m => m.MediaType == "audio")?.Url,
                    ImageUrl = eq.Question.Media?
                        .FirstOrDefault(m => m.MediaType == "image")?.Url,
                    Answers = eq.Question.Answers
                        .OrderBy(a => a.OrderIndex)
                        .ThenBy(a => a.Id) // Deterministic ordering
                        .Select(a => new AnswerOption
                        {
                            Id = a.Id,
                            Content = a.Content ?? string.Empty,
                            OrderIndex = a.OrderIndex,
                            // ❌ NEVER send IsCorrect!
                        })
                        .ToList()
                }).ToList()
            };
        }

        //  HELPER: Helper class for loading
        private class ExamQuestionWithAnswers
        {
            public Guid Id { get; set; }
            public Guid QuestionId { get; set; }
            public int OrderIndex { get; set; }
            public double Point { get; set; }
            public Question Question { get; set; }
        }
    }
}
