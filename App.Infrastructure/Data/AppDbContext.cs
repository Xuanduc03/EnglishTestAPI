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

            modelBuilder.Entity<ExamAnswer>(entity =>
            {
                entity.ToTable("exam_answer");
            });

            modelBuilder.Entity<ExamAttempt>(entity =>
            {
                entity.ToTable("exam_attempt");
            });

            modelBuilder.Entity<ExamSectionResult>(entity =>
            {
                entity.ToTable("exam_section_result");
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

                // Quan hệ 1-n: Exam -> ScoreTables
                entity.HasMany(e => e.ScoreTables)
                      .WithOne(s => s.Exam)
                      .HasForeignKey(s => s.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 2. ExamSection
            modelBuilder.Entity<ExamSection>(entity =>
            {
                entity.ToTable("exam_sections");
                entity.HasKey(e => e.Id);

                // --- CẤU HÌNH FIX LỖI CONFLICT (MULTIPLE CASCADE PATHS) ---

                // 1. Quan hệ với Exam: GIỮ CASCADE
                // Logic: Xóa đề thi (Exam) thì xóa luôn các phần thi (Section) là đúng.
                entity.HasOne(s => s.Exam)
                      .WithMany(e => e.Sections)
                      .HasForeignKey(s => s.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 2. Quan hệ với Category: TẮT CASCADE -> DÙNG RESTRICT
                // Logic: Xóa Category (VD: "Listening") thì KHÔNG ĐƯỢC xóa Section.
                // Phải báo lỗi chặn lại nếu Category đó đang được sử dụng.
                entity.HasOne(s => s.Category)
                      .WithMany() // Category không cần list ExamSections
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict); // <--- QUAN TRỌNG NHẤT LÀ DÒNG NÀY
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
                      .WithMany() // Question không cần biết nó nằm trong đề nào
                      .HasForeignKey(eq => eq.QuestionId)
                      .OnDelete(DeleteBehavior.Restrict); // Xóa câu hỏi gốc thì KHÔNG được xóa nếu đã có trong đề thi (để giữ lịch sử)
            });

            // 4. QuestionGroup Configuration
            modelBuilder.Entity<QuestionGroup>(entity =>
            {
                entity.ToTable("question_groups");
                entity.HasKey(e => e.Id);

                // --- 2. CẤU HÌNH NỘI DUNG (GIỮ NGUYÊN) ---
                // Lưu ý: Nếu dùng MySQL/MariaDB thì để longtext, SQL Server thì bỏ dòng HasColumnType hoặc để nvarchar(max)
                entity.Property(e => e.Content).HasColumnType("longtext");
                entity.Property(e => e.Transcript).HasColumnType("longtext");
                entity.Property(e => e.MediaJson).HasColumnType("longtext"); // Vẫn giữ để lưu metadata nếu cần

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
                // Khi xoá Bài đọc (Group) -> Xoá luôn các câu hỏi con bên trong -> Cascade là đúng
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

            modelBuilder.Entity<ExamResult>(entity =>
            {
                entity.ToTable("exam_results");
                entity.HasKey(e => e.Id);

                // Cấu hình lưu JSON
                entity.Property(e => e.ScoreDetailJson).HasColumnType("longtext");

                // --- CẤU HÌNH QUAN HỆ ĐỂ TRÁNH LỖI CIRCLE/CASCADE ---

                // 1. Quan hệ với Student: GIỮ CASCADE
                // Logic: Xóa học sinh thì xóa luôn kết quả thi của nó (Dọn rác sạch sẽ).
                entity.HasOne(r => r.Student)
                      .WithMany() // Student có thể không cần list ExamResults
                      .HasForeignKey(r => r.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 2. Quan hệ với Exam: TẮT CASCADE -> DÙNG RESTRICT
                // Logic: Xóa Đề thi (Exam) thì KHÔNG ĐƯỢC xóa kết quả nếu đã có người làm.
                // Admin phải xóa thủ công các lượt thi trước, hoặc chỉ ẩn đề thi đi thôi.
                entity.HasOne(r => r.Exam)
                      .WithMany() // Exam có thể không cần list ExamResults
                      .HasForeignKey(r => r.ExamId)
                      .OnDelete(DeleteBehavior.Restrict); // <--- QUAN TRỌNG: Chặn lỗi tại đây

                // 3. Quan hệ với StudentAnswers (Con của Result)
            });

            modelBuilder.Entity<QuestionTag>(entity =>
            {
                entity.ToTable("question_tags");
                entity.HasKey(e => e.Id);

                // 1. Cấu hình độ dài và ràng buộc
                entity.Property(e => e.Tag)
                      .IsRequired()
                      .HasMaxLength(100); // Giới hạn 100 ký tự cho tên Tag

                entity.Property(e => e.TagType)
                      .HasMaxLength(50);  // VD: "Topic", "Grammar"

                // 2. ĐÁNH INDEX (Rất quan trọng cho tốc độ tìm kiếm)
                // Giúp query kiểu: Tìm tất cả câu hỏi có tag "Present Simple" chạy nhanh hơn
                entity.HasIndex(e => e.Tag);
                entity.HasIndex(e => e.TagType);

                // 3. CẤU HÌNH QUAN HỆ (RELATIONSHIPS)

                // 3.1. Quan hệ với Câu hỏi lẻ (Question)
                // Logic: Nếu xóa Câu hỏi -> Xóa luôn các Tag dán trên câu hỏi đó
                entity.HasOne(t => t.Question)
                      .WithMany(q => q.Tags)
                      .HasForeignKey(t => t.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade); // 🔥 Cascade là bắt buộc

                // 3.2. Quan hệ với Bài đọc (QuestionGroup)
                // Logic: Nếu xóa Bài đọc -> Xóa luôn các Tag dán trên bài đọc đó
                entity.HasOne(t => t.QuestionGroup)
                      .WithMany(g => g.Tags)
                      .HasForeignKey(t => t.QuestionGroupId)
                      .OnDelete(DeleteBehavior.Cascade); // 🔥 Cascade là bắt buộc
            });

            // 7. ScoreTable
            modelBuilder.Entity<ScoreTable>(entity => {
                entity.ToTable("score_tables");
                // Lưu JSON dài
                entity.Property(e => e.ConversionJson).HasColumnType("longtext");
            });

            modelBuilder.Entity<ScoreTable>(entity =>
            {
                entity.ToTable("score_tables");

                entity.Property(e => e.ConversionJson)
                      .HasColumnType("longtext");

                entity.HasOne(s => s.Category)
                      .WithMany()
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
