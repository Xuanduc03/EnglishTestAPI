using App.Domain.Domain.Logging;
using App.Domain.Domain.Training;
using App.Domain.Entities;
using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace App.Application.Interfaces
{
    public interface IAppDbContext : IBaseDbContext
    {
        DbSet<User> Users { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<PasswordResetToken> PasswordResetTokens { get; }
        DbSet<Category> Categories { get; }
        DbSet<Student> Students { get; }

        // --- KHỐI EXAM ---
        DbSet<Exam> Exams { get; }
        DbSet<ExamSection> ExamSections { get; }
        DbSet<ExamQuestion> ExamQuestions { get; }
        DbSet<ScoreTable> ScoreTables { get; }
        DbSet<ScoreTableEntry> ScoreTableEntries { get; }

        // --- KHỐI QUESTION BANK ---
        DbSet<Question> Questions { get; }
        DbSet<QuestionGroup> QuestionGroups { get; }
        DbSet<Answer> Answers { get; }
        DbSet<QuestionMedia> QuestionMedias { get; }
        DbSet<QuestionGroupMedia> QuestionGroupMedia { get; }
        DbSet<QuestionTag> QuestionTags { get; }

        // --- KHỐI KẾT QUẢ ---
        DbSet<ExamResult> ExamResults { get; }
        DbSet<ExamAnswer> ExamAnswers { get; }
        DbSet<ExamAttempt> ExamAttempts { get; }
        DbSet<ExamSectionResult> ExamSectionResults { get; }

        // --- KHỐI LUYỆN THI ---
        DbSet<PracticeAttempt> PracticeAttempts { get; }
        DbSet<PracticeAnswer> PracticeAnswers { get; }
        DbSet<PracticePartResult> PracticePartResults { get; }

        // --- KHỐI TỪ VỰNG & LOG ---
        DbSet<VocabularyWord> VocabularyWords { get; }
        DbSet<UserVocabularyProgress> UserVocabularyProgresses { get; }
        DbSet<UserStatistics> UserStatistics { get; }
        DbSet<ActivityLog> ActivityLogs { get; }
        DbSet<ExamActivityLog> ExamActivityLogs { get; }

        // Các hàm thực thi cốt lõi
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        DatabaseFacade Database { get; }
    }
}
