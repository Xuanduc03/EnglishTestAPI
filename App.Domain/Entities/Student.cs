

namespace App.Domain.Entities
{
    public class Student : BaseEntity
    {
        public string Fullname { get; set; }
        public string? CCCD { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? SBD { get; set; }
        public int Streak { get; set; } // chuỗi học
        public DateTime? LastStreakDate { get; set; } // tính ngày sử dụng gần nhất
        public int Points { get; set; }
        public string MemberLevel { get; set; }
        public Guid UserId { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual User User { get; set; }

    }
}
