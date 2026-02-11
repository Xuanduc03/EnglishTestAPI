using App.Application.DTO;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Students.Queries
{
    public record GetStudentProfile(Guid UserId) : IRequest<StudentProfileDto>;

    public class GetStudentProfileHandler : IRequestHandler<GetStudentProfile, StudentProfileDto>
    {
        private readonly IAppDbContext _dbContext;

        public GetStudentProfileHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StudentProfileDto> Handle(GetStudentProfile request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == request.UserId)
                .Where(u => u.StudentProfile != null)
                .Select(u => new StudentProfileDto
                {
                    // user
                    UserId = u.Id,
                    Email = u.Email,
                    Phone = u.Phone,
                    AvatarUrl = u.AvatarUrl,
                    LastLogin = u.LastLogin,
                    CreatedAt = u.CreatedAt,

                    // Student
                    Fullname = u.StudentProfile!.Fullname,
                    Gender = u.StudentProfile.Gender,
                    BirthDate = u.StudentProfile.DateOfBirth,
                    Streak = u.StudentProfile.Streak,
                    Points = u.StudentProfile.Points,
                    MemberLevel = u.StudentProfile.MemberLevel,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                throw new KeyNotFoundException("Người dùng không tồn tại");

            return user;
        }
    }
}
