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
    // GET USER PERMISSIONS
    public record GetUserPermissionsQuery(Guid UserId) : IRequest<List<PermissionDto>>;

    public class GetUserPermissionsQueryHandler : IRequestHandler<GetUserPermissionsQuery, List<PermissionDto>>
    {
        private readonly IAppDbContext _dbContext;

        public GetUserPermissionsQueryHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<PermissionDto>> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var permissions = await _dbContext.Users
                    .Where(u => u.Id == request.UserId)
                    .SelectMany(u => u.UserRoles)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                    })
                    .Distinct()
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Name)
                    .ToListAsync(cancellationToken);

                return permissions;
            }
            catch (Exception ex)
            {
                throw new Exception("Có lỗi xảy ra khi lấy quyền của người dùng");
            }
        }
    }
}
