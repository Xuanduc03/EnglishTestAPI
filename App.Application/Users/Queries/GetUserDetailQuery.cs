using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Users.Queries
{
    public record GetUserDetailQuery(Guid UserId) : IRequest<UserDetailDto>;
    public class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, UserDetailDto>
    {
        private readonly IAppDbContext _dbContext;
        public GetUserDetailQueryHandler(IAppDbContext dbContext, ILogger<GetUserDetailQueryHandler> logger)
        {
            _dbContext = dbContext;
        }
        public async Task<UserDetailDto> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _dbContext.Users
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(u => u.Id == request.UserId)
                    .Select(u => new UserDetailDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Fullname = u.FullName,
                        Phone = u.Phone,
                        IsActive = u.IsActive,
                        IsDeleted = u.IsDeleted,
                        FailedLoginAttempts = u.FailedLoginAttempts,
                        LockoutEnd = u.LockoutEnd,
                        LastLogin = u.LastLoginAt,
                        Role = u.Role,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    throw new KeyNotFoundException("Người dùng không tồn tại");
                }

                // TODO: Add statistics if you have Courses/Enrollments tables
                // user.Stats = new UserStatsDto
                // {
                //     EnrolledCoursesCount = await _dbContext.Enrollments.CountAsync(e => e.UserId == request.UserId),
                //     TeachingCoursesCount = await _dbContext.Courses.CountAsync(c => c.InstructorId == request.UserId),
                //     CompletedCoursesCount = await _dbContext.Enrollments.CountAsync(e => e.UserId == request.UserId && e.IsCompleted)
                // };
                return user;
            }

            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
