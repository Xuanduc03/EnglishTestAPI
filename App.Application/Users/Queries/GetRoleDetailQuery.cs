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
    // GET ROLE DETAIL
    public record GetRoleDetailQuery(Guid RoleId) : IRequest<RoleDetailDto>;

    public class GetRoleDetailQueryHandler : IRequestHandler<GetRoleDetailQuery, RoleDetailDto>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<GetRoleDetailQueryHandler> _logger;

        public GetRoleDetailQueryHandler(IAppDbContext dbContext, ILogger<GetRoleDetailQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<RoleDetailDto> Handle(GetRoleDetailQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _dbContext.Roles
                    .AsNoTracking()
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Where(r => r.Id == request.RoleId)
                    .Select(r => new RoleDetailDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        UserCount = r.UserRoles.Count,
                        Permissions = r.RolePermissions.Select(rp => new PermissionDto
                        {
                            Id = rp.Permission.Id,
                            Name = rp.Permission.Name,
                            Description = rp.Permission.Description,
                            Module = rp.Permission.Module
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (role == null)
                {
                    throw new KeyNotFoundException("Vai trò không tồn tại");
                }

                return role;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Có lỗi xảy ra khi lấy thông tin vai trò");
            }
        }
    }
}
