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
    // GET ALL PERMISSIONS
    public record GetPermissionsQuery(string? Module = null) : IRequest<List<PermissionDto>>;

    public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, List<PermissionDto>>
    {
        private readonly IAppDbContext _dbContext;
        private readonly ILogger<GetPermissionsQueryHandler> _logger;

        public GetPermissionsQueryHandler(IAppDbContext dbContext, ILogger<GetPermissionsQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.Permissions.AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.Module))
                {
                    query = query.Where(p => p.Module == request.Module);
                }

                var permissions = await query
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Name)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Module = p.Module
                    })
                    .ToListAsync(cancellationToken);

                return permissions;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
