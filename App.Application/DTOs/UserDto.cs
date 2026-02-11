using App.Domain.Entities;
using AutoMapper;
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
        public List<Guid>? RoleIds { get; set; }
    }


    // update user dto
    public class UpdateUserDto
    {
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? Phone { get; set; }
        public string? NewPassword { get; set; }
        public bool? IsActive { get; set; }
        public List<Guid>? RoleIds { get; set; }
    }


    // list user dto
    public class UserListDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<RoleDto> Roles { get; set; } = new();
    }


    // detail user dto 
    public class UserDetailDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<RoleDetailDto> Roles { get; set; }
        public List<string> Permissions { get; set; }
        public UserStatsDto? Stats { get; set; }
    }



    public class UserStatsDto
    {
        public int EnrolledCoursesCount { get; set; }
        public int TeachingCoursesCount { get; set; }
        public int CompletedCoursesCount { get; set; }
    }


    public class CreateRoleDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public List<Guid>? PermissionIds { get; set; }
    }

    public class UpdateRoleDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<Guid>? PermissionIds { get; set; }
    }
    public class RoleDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UserCount { get; set; }
        public DateTime? AssignedAt { get; set; }
        public List<PermissionDto> Permissions { get; set; }
    }


    // rolte for user dto
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public List<PermissionDto>? Permissions { get; set; }
    }

    // Permission DTOs
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Module { get; set; }
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

        // Roles
        public List<string> Roles { get; set; } = new();
        public string PrimaryRole { get; set; }

        // Profiles
        public bool HasStudentProfile { get; set; }
        public bool HasTeacherProfile { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? TeacherId { get; set; }
    }

    public class RoleSelectDto
    {
        public Guid Value { get; set; }   // value = roleId
        public string Label { get; set; } // label = hiển thị
    }

    // Assign Roles
    public class AssignRolesDto
    {
        public List<Guid> RoleIds { get; set; }
    }

    // Assign Permissions
    public class AssignPermissionsDto
    {
        public List<Guid> PermissionIds { get; set; }
    }

    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // ===== User =====
            CreateMap<User, UserListDto>()
                .ForMember(d => d.Roles,
                    o => o.MapFrom(s =>
                        s.UserRoles.Select(ur => new RoleDto
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            Description = ur.Role.Description,
                            AssignedAt = ur.CreatedAt
                        })
                    ));

            CreateMap<User, UserDetailDto>()
                .ForMember(d => d.Roles,
                    o => o.MapFrom(s =>
                        s.UserRoles.Select(ur => new RoleDetailDto
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            Description = ur.Role.Description,
                            AssignedAt = ur.CreatedAt
                        })
                    ))
                .ForMember(d => d.Permissions,
                    o => o.MapFrom(s =>
                        s.UserRoles
                            .SelectMany(ur => ur.Role.RolePermissions)
                            .Select(rp => rp.Permission.Name)
                            .Distinct()
                    ));


            CreateMap<UpdateUserDto, User>()
                .ForAllMembers(o =>
                    o.Condition((_, _, src) => src != null));
        }
    }
}
