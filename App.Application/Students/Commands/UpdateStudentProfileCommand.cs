using App.Application.DTO;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;


namespace App.Application.Students.Commands
{
    public record UpdateStudentProfileCommand(
       Guid UserId,
       string? Fullname,
       string? Phone,
       string? Gender,
       DateTime? BirthDate,
       string? AvatarUrl = null,
       string? AvatarPublicId = null,
       IFormFile? AvatarFile = null  // Thêm IFormFile vào command
   ) : IRequest<StudentProfileDto>;

    public class UpdateStudentProfileCommandHandler : IRequestHandler<UpdateStudentProfileCommand, StudentProfileDto>
    {
        private readonly IAppDbContext _dbContext;

        public UpdateStudentProfileCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StudentProfileDto> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                throw new KeyNotFoundException("User không tồn tại");

            if (user.StudentProfile == null)
                throw new UnauthorizedAccessException("User không phải là student");
            if (!user.IsActive || !user.StudentProfile.IsActive)
                throw new UnauthorizedAccessException("Hồ sơ học viên không hoạt động");

            var phone = request.Phone?.Trim();
            if (!string.IsNullOrWhiteSpace(phone))
            {
                if (!Regex.IsMatch(phone, @"^0\d{9,10}$"))
                    throw new ArgumentException("Số điện thoại không hợp lệ", nameof(request.Phone));
                if (phone != user.Phone && await _dbContext.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Id != user.Id && u.Phone == phone, cancellationToken))
                    throw new InvalidOperationException("Số điện thoại đã được sử dụng");
            }

            // 1️⃣ Update USER fields
            user.UpdateProfile(user.Email, request.Fullname ?? user.FullName, phone ?? user.Phone);

            user.UpdatedAt = DateTime.UtcNow;

            // 2️⃣ Update STUDENT fields

            Gender? gender = user.StudentProfile.Gender;
            if (!string.IsNullOrWhiteSpace(request.Gender))
            {
                if (!Enum.TryParse<Gender>(request.Gender, true, out var parsedGender)
                    || !Enum.IsDefined(parsedGender))
                    throw new ArgumentException("Giới tính không hợp lệ", nameof(request.Gender));
                gender = parsedGender;
            }
            user.StudentProfile.UpdateProfile(gender, request.BirthDate, request.AvatarUrl, request.AvatarPublicId);
            user.StudentProfile.Fullname = user.FullName;

            user.StudentProfile.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Return DTO
            return new StudentProfileDto
            {
                UserId = user.Id,
                Email = user.Email,
                Fullname = user.FullName,
                Phone = user.Phone,
                AvatarUrl = user.StudentProfile.AvatarUrl,
                UpdatedAt = user.StudentProfile.UpdatedAt,
                Gender = user.StudentProfile.Gender?.ToString(),
                BirthDate = user.StudentProfile.DateOfBirth,
                Streak = user.StudentProfile.Streak,
                Points = user.StudentProfile.Points,
                MemberLevel = user.StudentProfile.MemberLevel.ToString(),
                LastLogin = user.LastLoginAt,
                CreatedAt = user.StudentProfile.CreatedAt,
            };
        }
    }
}
