using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Users.Queries
{
    // GET ALL ROLES
    public record GetRolesQuery(bool IncludePermissions = false) : IRequest<List<RoleDto>>;

    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<GetRolesQueryHandler> _logger;

        public GetRolesQueryHandler(IAppDbContext dbContext, ILogger<GetRolesQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.Roles.AsQueryable();

                if (request.IncludePermissions)
                {
                    query = query.Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission);
                }

                var roles = await query
                    .OrderBy(r => r.Name)
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        CreatedAt = r.CreatedAt,
                        Permissions = request.IncludePermissions
                            ? r.RolePermissions.Select(rp => new PermissionDto
                            {
                                Id = rp.Permission.Id,
                                Name = rp.Permission.Name,
                                Description = rp.Permission.Description,
                            }).ToList()
                            : null
                    })
                    .ToListAsync(cancellationToken);

                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                throw new Exception("Có lỗi xảy ra khi lấy danh sách vai trò");
            }
        }
    }
}
