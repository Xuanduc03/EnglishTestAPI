using App.Domain.Entities;
using AutoMapper;

namespace App.Application.Students
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Gender? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public int Streak { get; set; }
        public DateTime? LastStreakDate { get; set; }
        public int Points { get; set; }
        public MemberLevel MemberLevel { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateStudentDto
    {
        public Guid UserId { get; set; }
        public Gender? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }


    public class UpdateStudentDto
    {
        public Gender? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
    }
    // ===== PROFILE =====
    public class StudentProfileDto : StudentDto
    {
        public int Rank { get; set; }
        public int TotalExams { get; set; }
        public double AverageScore { get; set; }
        public StreakInfoDto StreakInfo { get; set; }
    }

    public class StreakInfoDto
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime? LastStreakDate { get; set; }
        public bool CheckedInToday { get; set; }
    }

    // ===== FILTER =====
    public class StudentFilter
    {
        public string? Keyword { get; set; }
        public MemberLevel? MemberLevel { get; set; }
        public int? MinPoints { get; set; }
        public int? MinStreak { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ExamHistoryFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? ExamId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Bento Dashboard user info dto
    public class DashboardInfoDto
    {
        public string Name { get; set; }
        public string Rank { get; set; }
        public int CurrentScore { get; set; }
        public int TargetScore { get; set; }
        public int Streak { get; set; }
        public List<bool> StreakHistory { get; set; } // 7 ngày gần nhất
    }

    public class StudentMappingProfile : Profile
    {
        public StudentMappingProfile()
        {
            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email,
                    opt => opt.MapFrom(src => src.User.Email));

            CreateMap<CreateStudentDto, Student>();

            CreateMap<UpdateStudentDto, Student>()
                .ForAllMembers(opt => opt.Condition(
                    (src, dest, srcMember) => srcMember != null));
        }
    }
}