using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Interfaces
{
    public interface IAppDbContext : IBaseDbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Student> Students { get; set; }

        // --- KHỐI EXAM  ---
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamSection> ExamSections { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ScoreTable> ScoreTables { get; set; }
        public DbSet<ExamStructureItem> ExamStructures { get; set; }

        // --- KHỐI QUESTION BANK  ---
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionGroup> QuestionGroups { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<QuestionGroupMedia> QuestionGroupMedia { get; set; }
        public DbSet<QuestionMedia> QuestionMedias { get; set; }
        public DbSet<QuestionTag> QuestionTags { get; set; }

        // --- KHỐI KẾT QUẢ ---
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }

        public DbSet<PracticeAttempt> PracticeAttempts { get; set; }
        public DbSet<PracticeAnswer> PracticeAnswers { get; set; }
        public DbSet<PracticePartResult> PracticePartResults { get; set; }
    }
}
