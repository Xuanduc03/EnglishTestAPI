namespace App.Domain.Entities
{
    // 
    public class UserRole : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.Now;
        public Guid AssignedBy { get; set; }
        public User User { get; set; }
        public Role Role { get; set; }
    }
}