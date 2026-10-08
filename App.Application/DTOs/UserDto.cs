using App.Domain.Entities;
using AutoMapper;
using App.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    // create user dto
    public class CreateUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Fullname { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public UserRole Role { get; set; } = UserRole.Student;
    }


    // update user dto
    public class UpdateUserDto
    {
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? Phone { get; set; }
        public string? NewPassword { get; set; }
        public bool? IsActive { get; set; }
        public UserRole? Role { get; set; }
    }


    // list user dto
    public class UserListDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? CreatedAt { get; set; }
        public UserRole Role { get; set; }
    }


    // detail user dto 
    public class UserDetailDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserRole Role { get; set; }

        public UserStatsDto? Stats { get; set; }
    }



    public class UserStatsDto
    {
        public int EnrolledCoursesCount { get; set; }
        public int TeachingCoursesCount { get; set; }
        public int CompletedCoursesCount { get; set; }
    }



    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public UserRole Role { get; set; }

        // Profiles
        public bool HasStudentProfile { get; set; }
        public bool HasTeacherProfile { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? TeacherId { get; set; }
    }

    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserListDto>()
                .ForMember(d => d.Fullname, o => o.MapFrom(s => s.FullName))
                .ForMember(d => d.LastLogin, o => o.MapFrom(s => s.LastLoginAt));

            CreateMap<User, UserDetailDto>()
                .ForMember(d => d.Fullname, o => o.MapFrom(s => s.FullName))
                .ForMember(d => d.LastLogin, o => o.MapFrom(s => s.LastLoginAt));
        }
    }
}
