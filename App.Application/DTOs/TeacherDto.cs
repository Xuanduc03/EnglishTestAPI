using App.Domain.Entities;

namespace App.Application.DTOs
{
    public class TeacherDto
    {
        public Guid Id { get; set; }
        public string Fullname { get; set; }
        public string? Specialty { get; set; }
        public string? Degree { get; set; }
        public string? Experience { get; set; }
        public string? Bio { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UserId { get; set; }

        // User information
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Username { get; set; }

        // Statistics
        public int TotalClasses { get; set; }
        public int ActiveClasses { get; set; }
    }

    public class CreateTeacherDto
    {
        public string Fullname { get; set; }
        public string? Specialty { get; set; }
        public string? Degree { get; set; }
        public string? Experience { get; set; }
        public string? Bio { get; set; }
        public Guid UserId { get; set; }

        // User creation data
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string? Phone { get; set; }
    }

    public class UpdateTeacherDto
    {
        public string Fullname { get; set; }
        public string? Specialty { get; set; }
        public string? Degree { get; set; }
        public string? Experience { get; set; }
        public string? Bio { get; set; }
        public Guid UpdatedBy { get; set; }
    }

    public class TeacherListDto
    {
        public Guid Id { get; set; }
        public string Fullname { get; set; }
        public string? Specialty { get; set; }
        public string? Degree { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public int TotalClasses { get; set; }
        public int ActiveClasses { get; set; }
    }

    public class TeacherDetailDto : TeacherDto
    {
        public List<TeacherClassDto> Classes { get; set; } = new();
        public UserDto User { get; set; }
    }

    public class TeacherClassDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Role { get; set; } // Main teacher, assistant, etc.
        public DateTime AssignedAt { get; set; }
        // Mới thêm
        public string? CategoryName { get; set; }
        public int StudentCount { get; set; }
        public int MaxStudents { get; set; }
        public decimal TuitionFee { get; set; }
        public string? ScheduleInfo { get; set; }
        public string? Room { get; set; }
        public string? Location { get; set; }
    }
}