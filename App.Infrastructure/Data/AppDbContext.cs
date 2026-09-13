using App.Domain.Entities;
using App.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using App.Infrastructure.Shares;
using App.Application.Services.Interface;
using App.Domain.Domain.Training;
using App.Domain.Identity;
using App.Domain.Domain.Logging;

namespace App.Infrastructure.Data
{
    public class AppDbContext : BaseDbContext<AppDbContext>, IAppDbContext
    {
        private readonly ICurrentUserService _currentUserService;
        public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) 
            : base(options, currentUserService) 
        { 
            _currentUserService = currentUserService;
        }
        public DbSet<User> Users { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Student> Students { get; set; }

        // --- KHỐI EXAM ---
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamSection> ExamSections { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ScoreTable> ScoreTables { get; set; } 

        // --- KHỐI QUESTION BANK ---
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionGroup> QuestionGroups { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<QuestionMedia> QuestionMedias { get; set; }
        public DbSet<QuestionGroupMedia> QuestionGroupMedia { get; set; }
        public DbSet<QuestionTag> QuestionTags { get; set; }

        // --- KHỐI KẾT QUẢ ---
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<ExamAnswer> ExamAnswers { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<ExamSectionResult> ExamSectionResults { get; set; }

        // khối luyện thi 
        public DbSet<PracticeAttempt> PracticeAttempts { get; set; }
        public DbSet<PracticeAnswer> PracticeAnswers { get; set; }
        public DbSet<PracticePartResult> PracticePartResults { get; set; }

        // Khối từ vựng 
        public DbSet<VocabularyWord> VocabularyWords { get; set; }
        public DbSet<UserVocabularyProgress> UserVocabularyProgresses { get; set; }
        public DbSet<UserStatistics> UserStatistics { get; set; }

        public DbSet<ScoreTableEntry> ScoreTableEntries { get; set; }

        // Loging 
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<ExamActivityLog> ExamActivityLogs { get; set; }
        protected override Guid? GetCurrentUserId() => _currentUserService.UserId;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
