

using App.Application.DTOs;
using App.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTO
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string Fullname { get; set; }
        public string? CCCD { get; set; }
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? SBD { get; set; }
        public string? School { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid UserId { get; set; }

        // User information
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Username { get; set; }

        // Statistics
        public int TotalClasses { get; set; }
        public int ActiveClasses { get; set; }
        public int CompletedClasses { get; set; }
    }

    public class CreateStudentDto
    {
        public string Fullname { get; set; }
        public string? CCCD { get; set; }
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? SBD { get; set; }
        public string? School { get; set; }
        public Guid UserId { get; set; }

        // User creation data (if creating user simultaneously)
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string? Phone { get; set; }
    }

    public class UpdateStudentDto
    {
        public string Fullname { get; set; }
        public string? CCCD { get; set; }
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? SBD { get; set; }
        public string? School { get; set; }
        public Guid UpdatedBy { get; set; }
    }

    public class StudentListDto
    {
        public Guid Id { get; set; }
        public string Fullname { get; set; }
        public string? CCCD { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? SBD { get; set; }
        public string? School { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int TotalClasses { get; set; }
        public int ActiveClasses { get; set; }
    }

    public class StudentDetailDto : StudentDto
    {
        public UserDto User { get; set; }
    }

    // UserProfileDto.cs
    public class StudentProfileDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public int Streak { get; set; }
        public int Points { get; set; }
        public string MemberLevel { get; set; } // "Basic", "Premium", "VIP"
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}