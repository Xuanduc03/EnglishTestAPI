using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Users.Queries;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace App.Application.Auth.Queries
{
    public record GetUserProfileQuery(Guid UserId) : IRequest<UserDetailDto>;
    
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserDetailDto>
    {

        private readonly IAppDbContext _dbContext;
        public GetUserProfileQueryHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserDetailDto> Handle(GetUserProfileQuery request, CancellationToken cancellation)
        {
            try
            {
                var user = await _dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == request.UserId)
                    .Select(u => new UserDetailDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Fullname = u.Fullname,
                        Phone = u.Phone,
                        LockoutEnd = u.LockoutEnd,
                        LastLogin = u.LastLogin,
                        CreatedAt = u.CreatedAt,
                        IsActive = u.IsActive,
                        UpdatedAt = u.UpdatedAt,
                    })
                    .FirstOrDefaultAsync(cancellation);

                if (user == null)
                {
                    throw new KeyNotFoundException("Người dùng không tồn tại");
                }
             return user;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
