

namespace App.Domain.Entities
{
    public enum Gender
    {
        Female = 0,
        Male = 1,
        Other = 2
    }

    public enum MemberLevel
    {
        Standard = 0,
        Silver = 1,
        Gold = 2,
        Diamond = 3
    }
    public class Student : BaseEntity
    {
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AvatarPublicId { get; set; }

        public int Streak { get; set; } 
        public DateTime? LastStreakDate { get; set; }
        public int Points { get; set; } = 0;
        public MemberLevel MemberLevel { get; set; } = MemberLevel.Standard;

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = default!;
        public virtual ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
    }
}
