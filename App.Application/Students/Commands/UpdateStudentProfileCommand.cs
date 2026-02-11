using App.Application.DTO;
using App.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;


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

            // 1️⃣ Update USER fields
            if (!string.IsNullOrWhiteSpace(request.Fullname))
                user.Fullname = request.Fullname;

            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone;


            // Update avatar nếu có URL mới
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                user.AvatarUrl = request.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(request.AvatarPublicId))
                user.AvatarPublicId = request.AvatarPublicId;

            user.UpdatedAt = DateTime.UtcNow;

            // 2️⃣ Update STUDENT fields

            if (!string.IsNullOrWhiteSpace(request.Gender))
                user.StudentProfile.Gender = request.Gender;

            if (request.BirthDate.HasValue)
                user.StudentProfile.DateOfBirth = request.BirthDate.Value;

            user.StudentProfile.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Return DTO
            return new StudentProfileDto
            {
                UserId = user.StudentProfile.Id,
                Email = user.Email,
                Fullname = user.Fullname,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                UpdatedAt = user.StudentProfile.UpdatedAt,
                Gender = user.StudentProfile.Gender,
                BirthDate = user.StudentProfile.DateOfBirth,
                Streak = user.StudentProfile.Streak,
                Points = user.StudentProfile.Points,
                MemberLevel = user.StudentProfile.MemberLevel,
                LastLogin = user.LastLogin,
                CreatedAt = user.StudentProfile.CreatedAt,
            };
        }
    }
}
