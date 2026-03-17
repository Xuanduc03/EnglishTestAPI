using App.Domain.Entities;
using App.Application.Interfaces;
using App.Domain.Shares;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using App.Infrastructure.Shares;
using App.Infrastructure.Persistence.Configurations;
using App.Application.Services.Interface;

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
        public DbSet<Role> Roles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Student> Students { get; set; }

        // --- KHỐI EXAM (Mới thêm) ---
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamSection> ExamSections { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ScoreTable> ScoreTables { get; set; } 

        // --- KHỐI QUESTION BANK (Mới thêm) ---
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionGroup> QuestionGroups { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<QuestionMedia> QuestionMedias { get; set; }
        public DbSet<QuestionGroupMedia> QuestionGroupMedia { get; set; }
        public DbSet<QuestionTag> QuestionTags { get; set; }

        // --- KHỐI KẾT QUẢ (Mới thêm) ---
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
        protected override Guid? GetCurrentUserId() => _currentUserService.UserId;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);


            // ==============================  
            // User
            // ==============================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasIndex(u => u.Email).IsUnique();
                // bỏ default sql → để entity khởi tạo bằng C#
                entity.Property(u => u.CreatedAt);
                entity.Property(u => u.UpdatedAt);
            });

            // ==============================
            // Role
            // ==============================
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");

                entity.HasIndex(r => r.Name).IsUnique();

                entity.Property(r => r.CreatedAt);
            });

            // ==============================
            // UserRole
            // ==============================
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");

                entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

                entity.Property(ur => ur.AssignedAt);

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId);
                
                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId);
            });

            // ==============================
            // Permission
            // ==============================
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("permissions");

                entity.HasIndex(p => p.Name).IsUnique();

                entity.Property(p => p.CreatedAt);
            });
         

            // ==============================
            // RolePermission
            // ==============================
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("role_permissions");

                entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(rp => rp.RoleId);

                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionId);
            });


            // ==============================
            // Student
            // ==============================
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("students");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.Fullname)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.HasIndex(s => s.CCCD).IsUnique(false); // Có thể trùng nếu người nhập sai
            });


          

            // category builder
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("categories");

                entity.HasKey(c => c.Id);

                entity.HasIndex(c => c.Code).IsUnique(false);
                entity.HasIndex(c => c.CodeType);

                entity.HasOne(c => c.Parent)
                      .WithMany(c => c.Children)
                      .HasForeignKey(c => c.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


            // 1. Exam
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.ToTable("exams");
                entity.HasKey(e => e.Id);

                // Quan hệ 1-n: Exam -> Sections (Xóa đề xóa luôn Section)
                entity.HasMany(e => e.Sections)
                      .WithOne(s => s.Exam)
                      .HasForeignKey(s => s.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);

            });


            // 2. ExamSection
            modelBuilder.Entity<ExamSection>(entity =>
            {
                entity.ToTable("exam_sections");
                entity.HasKey(e => e.Id);

                entity.HasOne(s => s.Category)
                      .WithMany() 
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict); 
            });

            // THAY THẾ block ScoreTable cũ bằng block này
            modelBuilder.Entity<ScoreTable>(entity =>
            {
                entity.ToTable("score_tables");

                // Trỏ vào Category LISTENING hoặc READING (dùng chung toàn hệ thống)
                entity.HasOne(s => s.SkillCategory)
                      .WithMany()
                      .HasForeignKey(s => s.SkillCategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Mỗi Skill chỉ có 1 bảng quy đổi active
                entity.HasIndex(s => s.SkillCategoryId).IsUnique(false);
            });

            modelBuilder.Entity<ScoreTableEntry>(entity =>
            {
                entity.ToTable("score_table_entries");

                entity.HasKey(e => e.Id);

                // Số câu đúng + ScoreTableId phải unique
                entity.HasIndex(e => new { e.ScoreTableId, e.CorrectAnswers }).IsUnique();

                entity.HasOne(e => e.ScoreTable)
                      .WithMany(s => s.Entries)
                      .HasForeignKey(e => e.ScoreTableId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 3. ExamQuestion (Bảng trung gian quan trọng)
            modelBuilder.Entity<ExamQuestion>(entity =>
            {
                entity.ToTable("exam_questions");
                entity.HasKey(e => e.Id);

                entity.HasOne(eq => eq.ExamSection)
                      .WithMany(es => es.ExamQuestions)
                      .HasForeignKey(eq => eq.ExamSectionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(eq => eq.Question)
                      .WithMany()
                      .HasForeignKey(eq => eq.QuestionId)
                      .OnDelete(DeleteBehavior.Restrict); 
            });

            // 4. QuestionGroup Configuration
            modelBuilder.Entity<QuestionGroup>(entity =>
            {
                entity.ToTable("question_groups");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Content).HasColumnType("longtext");
                entity.Property(e => e.Transcript).HasColumnType("longtext");
                entity.Property(e => e.MediaJson).HasColumnType("longtext"); 

                // --- 3. CẤU HÌNH QUAN HỆ (QUAN TRỌNG) ---

                // 3.1. Quan hệ với Category (Part/Skill)
                entity.HasOne(g => g.Category)
                      .WithMany() // Category chung không cần list QuestionGroups
                      .HasForeignKey(g => g.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict); // An toàn: Xoá Category không tự xoá bài đọc

                // 3.2. Quan hệ với Difficulty (Độ khó) -> TRÁNH LỖI CASCADE
                entity.HasOne(g => g.Difficulty)
                      .WithMany()
                      .HasForeignKey(g => g.DifficultyId)
                      .OnDelete(DeleteBehavior.Restrict); // BẮT BUỘC RESTRICT

                // 3.3. Quan hệ với Câu hỏi con (Questions)
                entity.HasMany(g => g.Questions)
                      .WithOne(q => q.Group)
                      .HasForeignKey(q => q.GroupId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 3.4. Quan hệ với Media (File đính kèm)
                entity.HasMany(g => g.Media)
                      .WithOne(m => m.QuestionGroup)
                      .HasForeignKey(m => m.QuestionGroupId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 5. Answer
            modelBuilder.Entity<Answer>(entity =>
            {
                entity.ToTable("answers");
                entity.HasKey(e => e.Id);
            });


            // 6. Question Configuration
            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("questions");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Content).HasColumnType("longtext");
                entity.Property(e => e.Explanation).HasColumnType("longtext");

                entity.Property(e => e.QuestionType)
                      .HasConversion<string>();

                entity.Property(e => e.PromptTypes)
                      .HasConversion<string>();

                entity.HasOne(q => q.Category)
                      .WithMany()
                      .HasForeignKey(q => q.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(q => q.Difficulty)
                      .WithMany()
                      .HasForeignKey(q => q.DifficultyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExamResult>(entity =>
            {
                entity.ToTable("exam_results");
                entity.HasKey(e => e.Id);

                // Cấu hình lưu JSON
                entity.Property(e => e.ScoreDetailJson).HasColumnType("longtext");

                entity.HasOne(r => r.User)
                      .WithMany() 
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Exam)
                      .WithMany() 
                      .HasForeignKey(r => r.ExamId)
                      .OnDelete(DeleteBehavior.Restrict); 

            });

            modelBuilder.Entity<QuestionTag>(entity =>
            {
                entity.ToTable("question_tags");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Tag)
                      .IsRequired()
                      .HasMaxLength(100); // Giới hạn 100 ký tự cho tên Tag

                entity.Property(e => e.TagType)
                      .HasMaxLength(50);  // VD: "Topic", "Grammar"

                entity.HasIndex(e => e.Tag);
                entity.HasIndex(e => e.TagType);

                // 3. CẤU HÌNH QUAN HỆ (RELATIONSHIPS)

                entity.HasOne(t => t.Question)
                      .WithMany(q => q.Tags)
                      .HasForeignKey(t => t.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade); 

                entity.HasOne(t => t.QuestionGroup)
                      .WithMany(g => g.Tags)
                      .HasForeignKey(t => t.QuestionGroupId)
                      .OnDelete(DeleteBehavior.Cascade); 
            });

          

          

            modelBuilder.Entity<ExamAttempt>(entity =>
            {
                entity.ToTable("exam_attempts");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.VersionNumber).IsRowVersion();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.ExamAttempts)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Exam)
                      .WithMany(ex => ex.Attempts)  
                      .HasForeignKey(e => e.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExamSectionResult>(entity =>
            {
                // Tên bảng
                entity.ToTable("exam_section_results");

                // Primary key
                entity.HasKey(e => e.Id);

                // Columns
                entity.Property(e => e.TotalQuestions)
                      .IsRequired();

                entity.Property(e => e.CorrectAnswers)
                      .IsRequired();

                entity.Property(e => e.ConvertedScore);

                // FK -> ExamAttempt
                entity.HasOne(e => e.Attempt)
                      .WithMany(a => a.SectionResults)
                      .HasForeignKey(e => e.ExamAttemptId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index (1 attempt chỉ có 1 result cho mỗi section)
                entity.HasIndex(e => new { e.ExamAttemptId, e.ExamSectionId })
                      .IsUnique();
            });

            // Cấu hình table cho use statistic
            modelBuilder.Entity<UserStatistics>(entity =>
            {
                entity.ToTable("user_statistics");
                entity.HasKey(e => e.Id);

                // Ràng buộc unique: mỗi user chỉ có một bản ghi thống kê
                entity.HasIndex(e => e.UserId).IsUnique();

                // Khóa ngoại liên kết với User
                entity.HasOne(e => e.User)
                      .WithOne()
                      .HasForeignKey<UserStatistics>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            }); 

            modelBuilder.Entity<ExamAnswer>(entity =>
            {
                // Table name
                entity.ToTable("exam_answers");

                // Primary key
                entity.HasKey(e => e.Id);

                // Required fields
                entity.Property(e => e.ExamAttemptId)
                      .IsRequired();

                entity.Property(e => e.ExamQuestionId)
                      .IsRequired();

                entity.Property(e => e.QuestionId)
                      .IsRequired();

                entity.Property(e => e.IsAnswered)
                      .IsRequired();

                entity.Property(e => e.IsCorrect)
                      .IsRequired();

                entity.Property(e => e.Point)
                      .HasColumnType("decimal(5,2)");

                // FK -> ExamAttempt
                entity.HasOne(e => e.Attempt)
                      .WithMany(a => a.Answers)
                      .HasForeignKey(e => e.ExamAttemptId)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK -> ExamQuestion
                entity.HasOne(e => e.ExamQuestions)
                      .WithMany(q => q.ExamAnswers)
                      .HasForeignKey(e => e.ExamQuestionId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.GradingStatus)
                     .HasConversion<string>();

                entity.Property(e => e.TextAnswer)
                      .HasColumnType("longtext");

                entity.Property(e => e.AiFeedback)
                      .HasColumnType("longtext");

                entity.Property(e => e.AiScoreDetailJson)
                      .HasColumnType("longtext");
                // Index: 1 attempt chỉ có 1 answer cho mỗi question
                entity.HasIndex(e => new
                {
                    e.ExamAttemptId,
                    e.ExamQuestionId
                })
                .IsUnique();

                // Index để query nhanh
                entity.HasIndex(e => e.ExamAttemptId);
            });
        }
    }
}
