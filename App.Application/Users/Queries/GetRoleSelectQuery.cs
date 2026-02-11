using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Users.Queries
{
    public record GetRolesForSelectQuery : IRequest<List<RoleSelectDto>>;

    public class GetRolesForSelectQueryHandler
    : IRequestHandler<GetRolesForSelectQuery, List<RoleSelectDto>>
    {
        private readonly IAppDbContext _context;

        public GetRolesForSelectQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleSelectDto>> Handle(
            GetRolesForSelectQuery request,
            CancellationToken cancellationToken)
        {
                return await _context.Roles
                    .Where(r => r.IsDeleted != true) // nếu có
                    .OrderBy(r => r.Name)
                    .Select(r => new RoleSelectDto
                    {
                        Value = r.Id,
                        Label = r.Name // hoặc $"{r.Code} - {r.Name}"
                    })
                    .ToListAsync(cancellationToken);
        }
    }


}
